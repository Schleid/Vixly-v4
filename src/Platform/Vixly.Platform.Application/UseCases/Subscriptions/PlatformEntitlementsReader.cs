using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformEntitlementsReader.cs
//
// PURPOSE:
// What a tenant may use now: status, access, features, limits, non-payment and legal parts (Slices 3, 6, 8 and 9).
//
// RESPONSIBILITIES:
// - Read the entitlements of one tenant for staff, Operations and background jobs
//
// IMPORTANT NOTES:
// - No permission check here; callers check first
// - Same answer for staff and for Operations (D-K9)
// =========================================================
public sealed class PlatformEntitlementsReader
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformCollectionsQueries _collections;

    private readonly PlatformLegalGate _legal;

    public PlatformEntitlementsReader(
        PlatformSubscriptionAdministration administration,
        IPlatformCollectionsQueries collections,
        PlatformLegalGate legal)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<PlatformTenantEntitlements> ReadAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(platformTenantId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformPlanVersion? version = plan?.Versions.FirstOrDefault(item => item.Id == subscription!.PlanVersionId);
        IReadOnlyList<PlatformLimitItem> limits = await _administration.LimitsAsync(platformTenantId, version, null, cancellationToken);
        PlatformNonPaymentInfo? nonPayment = null;

        if (subscription?.Status is PlatformSubscriptionStatus.BlockedForNonPayment or PlatformSubscriptionStatus.PendingPayment)
        {
            IReadOnlyList<PlatformOverdueInvoiceItem> due = await _collections.ListOverdueInvoicesAsync(platformTenantId, _administration.UtcNow, cancellationToken);
            nonPayment = new PlatformNonPaymentInfo(
                subscription.BlockedUtc,
                due.GroupBy(item => item.CurrencyCode).Select(group => new PlatformMoneyItem(group.Key, group.Sum(item => item.Balance))).OrderBy(item => item.CurrencyCode).ToArray(),
                due.Count > 0);
        }

        // Terms overdue: the tenant's staff are read-only until the owner accepts (Slice 8, owner D-Lg6).
        PlatformTenantLegalState legal = await _legal.StateAsync(platformTenantId, cancellationToken);

        return new PlatformTenantEntitlements(
            platformTenantId,
            subscription?.Status,
            PlatformLegalGate.Cap(subscription?.AccessAt(_administration.UtcNow, await _administration.AccessRulesAsync(cancellationToken)) ?? PlatformSubscriptionAccess.None, legal),
            version?.Features.Select(item => item.Feature).OrderBy(feature => feature).ToArray() ?? Array.Empty<PlatformPlanFeature>(),
            limits.ToDictionary(item => item.Kind, item => item.Effective),
            nonPayment,
            legal.Status is Vixly.Platform.Domain.Legal.PlatformLegalStatus.Due or Vixly.Platform.Domain.Legal.PlatformLegalStatus.Overdue
                ? new PlatformLegalInfo(legal.Status, legal.DeadlineUtc, legal.RequiredVersion)
                : null);
    }
}
