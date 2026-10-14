using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformTenantSubscriptionDetail.cs
//
// PURPOSE:
// A tenant's current subscription, as the tenant page shows it.
//
// RESPONSIBILITIES:
// - Carry plan, version, status, term, licences, amounts, dates, scheduled change, cancellation, access, limits and features
//
// IMPORTANT NOTES:
// - Amounts are before tax (D-S9)
// - NewerVersionAtRenewal is true when the next period moves to a newer version (owner, D-S2)
// =========================================================
public sealed record PlatformTenantSubscriptionDetail(
    Guid PlatformSubscriptionId,
    Guid PlatformTenantId,
    Guid PlatformPlanId,
    string PlanCode,
    string PlanName,
    int VersionNumber,
    bool NewerVersionAtRenewal,
    PlatformSubscriptionStatus Status,
    PlatformSubscriptionTerm Term,
    int Licences,
    int ActiveLicences,
    decimal PricePerLicence,
    decimal PeriodAmount,
    decimal NextPeriodAmount,
    decimal SetupFee,
    bool SetupFeeCharged,
    string CurrencyCode,
    DateTimeOffset? TrialEndsUtc,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    bool CancelAtPeriodEnd,
    DateTimeOffset StatusChangedUtc,
    PlatformSubscriptionAccess Access,
    PlatformScheduledChangeItem? ScheduledChange,
    IReadOnlyList<PlatformLimitItem> Limits,
    IReadOnlyList<PlatformPlanFeature> Features,
    decimal CurrentPeriodDiscount,
    decimal NextPeriodNetAmount,
    bool SetupFeeOverridden,
    PlatformDiscountItem? Discount,
    PlatformDiscountItem? PendingDiscount,
    IReadOnlyList<PlatformOneTimeChargeItem> Charges);
