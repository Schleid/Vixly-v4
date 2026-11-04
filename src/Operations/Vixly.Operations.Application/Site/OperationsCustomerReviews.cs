using Vixly.Operations.Application.Plans;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsCustomerReviews.cs
//
// PURPOSE:
// Works out a customer's review state for the portal (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Read the company's setting, the plan, the customer's finished shipments and their review
//
// IMPORTANT NOTES:
// - Only customers with a shipment delivered or picked up may write one
// - The plan must include the customer portal
// =========================================================
public sealed class OperationsCustomerReviews
{
    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsPlanFeatures _features;

    public OperationsCustomerReviews(IOperationsSiteRepository site, IOperationsSettingsRepository settings, IOperationsPlanFeatures features)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public async Task<bool> AskedAsync(Guid tenantId, CancellationToken cancellationToken) =>
        (await _settings.FindAsync(cancellationToken))?.AskForReviews == true
        && (await _features.GetAsync(tenantId, cancellationToken)).Includes(OperationsPlanModules.CustomerPortal);

    public async Task<OperationsReviewState> StateAsync(OperationsCustomer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);

        bool asked = await AskedAsync(customer.TenantId, cancellationToken);
        Vixly.Operations.Domain.Branding.OperationsSiteTestimonial? review = await _site.FindCustomerReviewAsync(customer.Id, cancellationToken);
        bool eligible = asked && await _site.FindLastFinishedShipmentAsync(customer.Id, cancellationToken) is not null;
        return new OperationsReviewState(asked, eligible, review, eligible && review is null && customer.ReviewInviteDismissedUtc is null);
    }
}
