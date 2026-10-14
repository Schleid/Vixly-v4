using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSubscriptionMessage.cs
//
// PURPOSE:
// The payload of every subscription message (D-S11).
//
// RESPONSIBILITIES:
// - Carry ids, codes, amounts and the currency of a subscription change, for Billing and later planes
//
// IMPORTANT NOTES:
// - Never carries a reason or a person: those stay in history and audit
// - Amounts are before tax (D-S9); ChargeNow is the prorated amount owed now, PeriodAmount the full amount of the current period
// =========================================================
public sealed record PlatformSubscriptionMessage(
    Guid PlatformSubscriptionId,
    Guid PlatformTenantId,
    string EventCode,
    PlatformSubscriptionStatus Status,
    Guid PlatformPlanId,
    Guid PlanVersionId,
    PlatformSubscriptionTerm Term,
    int Licences,
    decimal ChargeNow,
    decimal SetupFee,
    decimal PeriodAmount,
    string CurrencyCode,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    DateTimeOffset OccurredUtc,
    decimal DiscountAmount = 0m,
    IReadOnlyDictionary<string, string>? Details = null);
