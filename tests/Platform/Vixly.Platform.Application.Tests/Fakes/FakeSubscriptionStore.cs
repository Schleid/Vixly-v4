using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeSubscriptionStore.cs
//
// PURPOSE:
// In-memory plans, subscriptions, overrides, licence counts and subscription mail.
//
// RESPONSIBILITIES:
// - Stand in for the subscription repositories, queries, policy and mailer
//
// IMPORTANT NOTES:
// - Writes are visible at once; the shared fake unit of work reports the save
// =========================================================
internal sealed class FakeSubscriptionStore :
    IPlatformPlanRepository,
    IPlatformSubscriptionRepository,
    IPlatformLimitOverrideRepository,
    IPlatformLicenceCounter,
    IPlatformSubscriptionMailer,
    IPlatformSubscriptionQueries
{
    public List<PlatformPlan> Plans { get; } = new();

    public List<PlatformSubscription> Subscriptions { get; } = new();

    public List<PlatformSubscriptionEvent> Events { get; } = new();

    public List<PlatformTenantLimitOverride> Overrides { get; } = new();

    public List<(string To, string Plan, decimal Before, decimal After, DateTimeOffset Effective)> PriceNotices { get; } = new();

    public int ActiveLicences { get; set; }


    public Task<PlatformPlan?> GetAsync(Guid platformPlanId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Plans.FirstOrDefault(plan => plan.Id == platformPlanId));
    }

    public Task<PlatformPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(Plans.FirstOrDefault(plan => plan.Code == code));
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(Plans.Any(plan => plan.Code == code));
    }

    public Task<IReadOnlyList<PlatformPlan>> ListChoosableAsync(bool publicOnly, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformPlan>>(Plans
            .Where(plan => plan.CanBeChosen && (!publicOnly || plan.IsPublic))
            .OrderBy(plan => plan.DisplayOrder)
            .ToList());
    }

    public void Add(PlatformPlan plan)
    {
        Plans.Add(plan);
    }

    public Task<PlatformSubscription?> GetCurrentAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Subscriptions.FirstOrDefault(item => item.PlatformTenantId == platformTenantId && item.IsCurrent));
    }

    Task<PlatformSubscription?> IPlatformSubscriptionRepository.GetAsync(Guid platformSubscriptionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Subscriptions.FirstOrDefault(item => item.Id == platformSubscriptionId));
    }

    public Task<IReadOnlyList<Guid>> ListDueAsync(DateTimeOffset utcNow, int max, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Subscriptions
            .Where(item => (item.Status == PlatformSubscriptionStatus.Trialing && item.TrialEndsUtc <= utcNow) || item.IsRenewalDue(utcNow))
            .Select(item => item.Id)
            .Take(max)
            .ToList());
    }

    public Task<IReadOnlyList<PlatformSubscription>> ListCurrentOnPlanAsync(Guid platformPlanId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformSubscription>>(Subscriptions.Where(item => item.PlatformPlanId == platformPlanId && item.IsCurrent).ToList());
    }

    public void Add(PlatformSubscription subscription)
    {
        Subscriptions.Add(subscription);
    }

    public void AddEvents(IEnumerable<PlatformSubscriptionEvent> events)
    {
        Events.AddRange(events);
    }

    public Task<IReadOnlyList<PlatformTenantLimitOverride>> ListOpenAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformTenantLimitOverride>>(Overrides.Where(item => item.PlatformTenantId == platformTenantId && item.IsOpen).ToList());
    }

    Task<PlatformTenantLimitOverride?> IPlatformLimitOverrideRepository.GetAsync(Guid overrideId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Overrides.FirstOrDefault(item => item.Id == overrideId));
    }

    public void Add(PlatformTenantLimitOverride limitOverride)
    {
        Overrides.Add(limitOverride);
    }

    public Task<int> CountActiveAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(ActiveLicences);
    }

    public Task SendPriceIncreaseAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        string planName,
        PlatformSubscriptionTerm term,
        decimal currentAmount,
        decimal newAmount,
        string currencyCode,
        DateTimeOffset effectiveUtc,
        CancellationToken cancellationToken)
    {
        PriceNotices.Add((recipient.Email.Value, planName, currentAmount, newAmount, effectiveUtc));
        return Task.CompletedTask;
    }

    public Task<PagedResult<PlatformPlanListItem>> ListPlansAsync(PlatformPlanListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        List<PlatformPlanListItem> items = Plans
            .Select(plan => new PlatformPlanListItem(
                plan.Id, plan.Code, plan.Code, plan.Status, plan.IsPublic, plan.DisplayOrder, plan.Current?.Number, plan.Draft is not null,
                plan.CurrencyCode, plan.Current?.MonthlyPricePerLicence, plan.Current?.AnnualPricePerLicence, 0))
            .ToList();

        return Task.FromResult(new PagedResult<PlatformPlanListItem>(items, page, items.Count));
    }

    public Task<PagedResult<PlatformSubscriptionListItem>> ListSubscriptionsAsync(PlatformSubscriptionListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformSubscriptionListItem>.Empty(page));
    }

    public Task<PagedResult<PlatformSubscriptionEventItem>> ListEventsAsync(Guid platformTenantId, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformSubscriptionEventItem>.Empty(page));
    }

    public Task<int> CountSubscribersAsync(Guid platformPlanId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Subscriptions.Count(item => item.PlatformPlanId == platformPlanId && item.IsCurrent));
    }

    public Task<IReadOnlyDictionary<Guid, string>> StaffNamesAsync(IEnumerable<Guid> platformUserIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }

    public Task<bool> HasUsedPromotionAsync(Guid platformTenantId, Guid promotionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Subscriptions.Any(item => item.PlatformTenantId == platformTenantId
            && item.Discounts.Any(discount => discount.Source == PlatformDiscountSource.Promotion && discount.SourceId == promotionId)));
    }

    public Task<IReadOnlyList<PlatformSubscription>> ListWithUnnoticedDiscountsAsync(int max, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformSubscription>>(Subscriptions
            .Where(item => item.ActiveDiscount is { EndNoticeSentUtc: null } discount && !discount.Terms.HasNoEnd)
            .Take(max)
            .ToArray());
    }

    public Task<PagedResult<PlatformPromotionListItem>> ListPromotionsAsync(PlatformPromotionListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformPromotionListItem>.Empty(page));
    }

    public Task<PagedResult<PlatformOfferListItem>> ListOffersAsync(PlatformOfferListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformOfferListItem>.Empty(page));
    }

    public List<PlatformApprovalItem> Approvals { get; } = new();

    public Task<IReadOnlyList<PlatformApprovalItem>> ListApprovalsAsync(IReadOnlyCollection<PlatformApprovalKind> kinds, string language, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformApprovalItem>>(Approvals.Where(item => kinds.Contains(item.Kind)).ToArray());
    }
}
