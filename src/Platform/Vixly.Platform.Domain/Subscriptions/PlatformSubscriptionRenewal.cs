namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionRenewal.cs
//
// PURPOSE:
// The outcome of one renewal of a subscription.
//
// RESPONSIBILITIES:
// - Report the history event, whether the subscription ended, the new period and its amount
//
// IMPORTANT NOTES:
// - Returned by PlatformSubscription.RenewIfDue; the use case publishes it for Billing (D-S11)
// - PeriodAmount is the list amount; DiscountAmount is taken off it. DiscountEnded is the
//   history event when the discount ran out at this renewal
// =========================================================
public sealed record PlatformSubscriptionRenewal(
    PlatformSubscriptionEvent Event,
    bool Ended,
    Guid PlanVersionId,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    decimal PeriodAmount,
    string CurrencyCode,
    decimal DiscountAmount = 0m,
    PlatformSubscriptionEvent? DiscountEnded = null);
