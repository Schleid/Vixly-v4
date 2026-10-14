using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformSubscriptionQueries.cs
//
// PURPOSE:
// Read-only views of plans and subscriptions.
//
// RESPONSIBILITIES:
// - Page the plans list with subscriber counts
// - Page the subscriptions list
// - Page a tenant subscription history
// - Name tenants and staff for display
//
// IMPORTANT NOTES:
// - Projections only; nothing here changes data
// - Every list is paged (P-1)
// =========================================================
public interface IPlatformSubscriptionQueries
{
    Task<PagedResult<PlatformPlanListItem>> ListPlansAsync(
        PlatformPlanListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PlatformSubscriptionListItem>> ListSubscriptionsAsync(
        PlatformSubscriptionListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PlatformSubscriptionEventItem>> ListEventsAsync(
        Guid platformTenantId,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PlatformPromotionListItem>> ListPromotionsAsync(
        PlatformPromotionListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PlatformOfferListItem>> ListOffersAsync(
        PlatformOfferListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformApprovalItem>> ListApprovalsAsync(
        IReadOnlyCollection<PlatformApprovalKind> kinds,
        string language,
        CancellationToken cancellationToken);

    Task<int> CountSubscribersAsync(Guid platformPlanId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> StaffNamesAsync(IEnumerable<Guid> platformUserIds, CancellationToken cancellationToken);
}
