using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSubscriptionRepository.cs
//
// PURPOSE:
// Loads and stores subscriptions and their history.
//
// RESPONSIBILITIES:
// - Find the current subscription of a tenant
// - List subscriptions due for the clock job
// - List the current subscriptions on a plan
// - Add subscriptions and history events
// - Say whether a tenant already used a promotion (D-S20)
// - List subscriptions whose active discount has an end and was not yet announced (D-S25)
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// - "Current" means not Ended; at most one per tenant (D-S3)
// =========================================================
public interface IPlatformSubscriptionRepository
{
    Task<PlatformSubscription?> GetCurrentAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PlatformSubscription?> GetAsync(Guid platformSubscriptionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListDueAsync(DateTimeOffset utcNow, int max, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformSubscription>> ListCurrentOnPlanAsync(Guid platformPlanId, CancellationToken cancellationToken);

    Task<bool> HasUsedPromotionAsync(Guid platformTenantId, Guid promotionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformSubscription>> ListWithUnnoticedDiscountsAsync(int max, CancellationToken cancellationToken);

    void Add(PlatformSubscription subscription);

    void AddEvents(IEnumerable<PlatformSubscriptionEvent> events);
}
