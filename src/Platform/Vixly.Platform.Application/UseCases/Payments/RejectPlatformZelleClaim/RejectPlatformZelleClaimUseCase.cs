using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.RejectPlatformZelleClaim;

// =========================================================
// FILE: RejectPlatformZelleClaimUseCase.cs
//
// PURPOSE:
// Staff could not find the Zelle money: closes the report and tells the customer why.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The reason is emailed to the customer; the note stays internal
// =========================================================
public sealed class RejectPlatformZelleClaimUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public RejectPlatformZelleClaimUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<RejectPlatformZelleClaimResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RejectPlatformZelleClaimRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.BillingRecordPayments, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RejectPlatformZelleClaimResponse.Failure(refused);
        }

        PlatformZelleClaim? claim = await _zelle.Store.GetClaimAsync(request.ClaimId, cancellationToken);

        if (claim is null)
        {
            return RejectPlatformZelleClaimResponse.Failure(PlatformZelleErrors.ClaimNotFound);
        }

        try
        {
            claim.Reject(request.Reason, request.Note, context.PlatformUserId, _zelle.UtcNow);
        }
        catch (DomainException exception)
        {
            return RejectPlatformZelleClaimResponse.Failure(exception.Error);
        }

        _zelle.Notice(claim, PlatformZelleNoticeKind.Rejected);
        _zelle.Payments.Audit(context, "platform.payments.zelle_claim_rejected", PlatformPermissions.BillingRecordPayments, nameof(PlatformZelleClaim), claim.Id,
            new { invoice = claim.InvoiceNumber, reason = claim.RejectReason });

        return await _zelle.SaveAsync(cancellationToken)
            ? RejectPlatformZelleClaimResponse.Success()
            : RejectPlatformZelleClaimResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
