using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.BillPlatformChargesNow;

// =========================================================
// FILE: BillPlatformChargesNowUseCase.cs
//
// PURPOSE:
// Invoices a subscription's pending one-time charges now instead of with the next invoice (D-B5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class BillPlatformChargesNowUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformInvoiceIssuer _issuer;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BillPlatformChargesNowUseCase(
        PlatformBillingAdministration billing,
        PlatformInvoiceIssuer issuer,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformTenantRepository tenants,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BillPlatformChargesNowResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        BillPlatformChargesNowRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.SubscriptionsCharges, requireStepUp: false, cancellationToken) is { } refused)
        {
            return BillPlatformChargesNowResponse.Failure(refused);
        }

        var tenant = await _tenants.GetAsync(request.PlatformTenantId, cancellationToken);
        var subscription = await _subscriptions.GetCurrentAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null || subscription is null)
        {
            return BillPlatformChargesNowResponse.Failure(PlatformCommonErrors.NotFound);
        }

        var charges = subscription.PendingCharges;

        if (charges.Count == 0)
        {
            return BillPlatformChargesNowResponse.Failure(PlatformBillingErrors.NothingToBill);
        }

        PlatformInvoice invoice;

        try
        {
            invoice = await _issuer.IssueAsync(
                tenant,
                subscription.Id,
                PlatformInvoiceKind.Charges,
                "charges:" + Guid.CreateVersion7().ToString("N"),
                charges[0].CurrencyCode,
                null,
                null,
                charges.Select(charge => new PlatformInvoiceDraftLine(
                    PlatformInvoiceLineKind.OneTimeCharge,
                    "ui.invoice_line.charge",
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["description"] = charge.Description },
                    1m,
                    charge.Amount,
                    charge.Id)).ToArray(),
                cancellationToken);
            subscription.MarkChargesBilled(charges.Select(charge => charge.Id).ToArray(), _billing.UtcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return BillPlatformChargesNowResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BillPlatformChargesNowResponse.Success(invoice.Id)
            : BillPlatformChargesNowResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
