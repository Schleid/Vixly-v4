using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformDiscountItem.cs
//
// PURPOSE:
// A discount on a subscription for the tenant page.
//
// RESPONSIBILITIES:
// - Carry the terms, where they came from, who granted them and how far they have run
//
// IMPORTANT NOTES:
// - LastsUntil is empty when the discount has no end or has not started
// =========================================================
public sealed record PlatformDiscountItem(
    Guid DiscountId,
    PlatformDiscountTermsItem Terms,
    PlatformDiscountSource Source,
    PlatformDiscountStatus Status,
    string Reason,
    string? GrantedByName,
    DateTimeOffset GrantedUtc,
    int PeriodsUsed,
    DateTimeOffset? LastsUntil)
{
    public static PlatformDiscountItem From(PlatformSubscriptionDiscount discount, PlatformSubscription subscription, IReadOnlyDictionary<Guid, string> names)
    {
        ArgumentNullException.ThrowIfNull(discount);
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(names);

        return new PlatformDiscountItem(
            discount.Id,
            PlatformDiscountTermsItem.From(discount.Terms),
            discount.Source,
            discount.Status,
            discount.Reason,
            discount.GrantedByUserId is Guid granter && names.TryGetValue(granter, out string? name) ? name : null,
            discount.CreatedUtc,
            discount.PeriodsUsed,
            discount.LastsUntil(subscription.PeriodEndUtc, subscription.Term));
    }
}
