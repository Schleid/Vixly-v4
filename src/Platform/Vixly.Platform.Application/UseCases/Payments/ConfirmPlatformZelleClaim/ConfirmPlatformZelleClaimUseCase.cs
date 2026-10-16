using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.ConfirmPlatformZelleClaim;

// =========================================================
// FILE: ConfirmPlatformZelleClaimUseCase.cs
//
// PURPOSE:
// Staff found the Zelle money: records a Zelle payment on the invoice and closes the report.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Same permission and step-up as recording a payment by hand
// - The payment rules apply: no overpayment, receipt numbered and emailed
// =========================================================
public sealed class ConfirmPlatformZelleClaimUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public ConfirmPlatformZelleClaimUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<ConfirmPlatformZelleClaimResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ConfirmPlatformZelleClaimRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.BillingRecordPayments, requireStepUp: true, cancellationToken) is { } refused)
        {
            return ConfirmPlatformZelleClaimResponse.Failure(refused);
        }

        PlatformZelleClaim? claim = await _zelle.Store.GetClaimAsync(request.ClaimId, cancellationToken);

        if (claim is null)
        {
            return ConfirmPlatformZelleClaimResponse.Failure(PlatformZelleErrors.ClaimNotFound);
        }

        if (!claim.IsWaiting)
        {
            return ConfirmPlatformZelleClaimResponse.Failure(PlatformZelleErrors.ClaimDecided);
        }

        PlatformInvoice? invoice = await _zelle.Payments.Billing.Invoices.GetAsync(claim.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return ConfirmPlatformZelleClaimResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        decimal amount = request.ReceivedAmount ?? claim.Amount;
        DateTimeOffset received = request.ReceivedUtc?.ToUniversalTime() ?? _zelle.UtcNow;

        try
        {
            PlatformInvoicePaymentResult result = await _zelle.RecordPaymentAsync(context, invoice, amount, received, claim.Reference, request.Note, cancellationToken);
            claim.Confirm(amount, received, result.Payment.Id, request.Note, context.PlatformUserId, _zelle.UtcNow);
        }
        catch (DomainException exception)
        {
            return ConfirmPlatformZelleClaimResponse.Failure(exception.Error);
        }

        _zelle.Payments.Audit(context, "platform.payments.zelle_claim_confirmed", PlatformPermissions.BillingRecordPayments, nameof(PlatformZelleClaim), claim.Id,
            new { invoice = claim.InvoiceNumber, reported = claim.Amount, received = amount });

        return await _zelle.SaveAsync(cancellationToken)
            ? ConfirmPlatformZelleClaimResponse.Success()
            : ConfirmPlatformZelleClaimResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
