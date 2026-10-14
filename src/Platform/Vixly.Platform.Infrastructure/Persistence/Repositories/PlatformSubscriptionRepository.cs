using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSubscriptionRepository.cs
//
// PURPOSE:
// Loads and stores subscriptions and their history.
//
// RESPONSIBILITIES:
// - Find the current subscription of a tenant
// - List what the clock must handle
// - Add subscriptions and events
//
// IMPORTANT NOTES:
// - "Current" means not Ended (D-S3)
// =========================================================
internal sealed class PlatformSubscriptionRepository : IPlatformSubscriptionRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSubscriptionRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformSubscription?> GetCurrentAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.Subscriptions.FirstOrDefaultAsync(
            subscription => subscription.PlatformTenantId == platformTenantId && subscription.Status != PlatformSubscriptionStatus.Ended,
            cancellationToken);
    }

    public Task<PlatformSubscription?> GetAsync(Guid platformSubscriptionId, CancellationToken cancellationToken)
    {
        return _context.Subscriptions.FirstOrDefaultAsync(subscription => subscription.Id == platformSubscriptionId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListDueAsync(DateTimeOffset utcNow, int max, CancellationToken cancellationToken)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Where(subscription =>
                (subscription.Status == PlatformSubscriptionStatus.Trialing && subscription.TrialEndsUtc <= utcNow)
                || ((subscription.Status == PlatformSubscriptionStatus.Active || subscription.Status == PlatformSubscriptionStatus.PastDue)
                    && subscription.PeriodEndUtc <= utcNow))
            .OrderBy(subscription => subscription.Id)
            .Select(subscription => subscription.Id)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformSubscription>> ListCurrentOnPlanAsync(Guid platformPlanId, CancellationToken cancellationToken)
    {
        return await _context.Subscriptions
            .Where(subscription => subscription.PlatformPlanId == platformPlanId && subscription.Status != PlatformSubscriptionStatus.Ended)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasUsedPromotionAsync(Guid platformTenantId, Guid promotionId, CancellationToken cancellationToken)
    {
        return _context.Subscriptions.AnyAsync(
            subscription => subscription.PlatformTenantId == platformTenantId
                && subscription.Discounts.Any(discount => discount.Source == PlatformDiscountSource.Promotion && discount.SourceId == promotionId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformSubscription>> ListWithUnnoticedDiscountsAsync(int max, CancellationToken cancellationToken)
    {
        return await _context.Subscriptions
            .Where(subscription => subscription.Discounts.Any(discount =>
                discount.Status == PlatformDiscountStatus.Active
                && discount.EndNoticeSentUtc == null
                && discount.Terms.Duration != PlatformDiscountDuration.NoEnd))
            .OrderBy(subscription => subscription.Id)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public void Add(PlatformSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        _context.Subscriptions.Add(subscription);
    }

    public void AddEvents(IEnumerable<PlatformSubscriptionEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        _context.SubscriptionEvents.AddRange(events);
    }
}
