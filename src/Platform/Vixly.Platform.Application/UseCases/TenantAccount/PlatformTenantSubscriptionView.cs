using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantSubscriptionView.cs
//
// PURPOSE:
// The tenant's own subscription as Operations shows it (call 19).
//
// RESPONSIBILITIES:
// - Carry what Operations shows the tenant
//
// IMPORTANT NOTES:
// - No staff names, discount reasons or override reasons
// =========================================================
public sealed record PlatformTenantSubscriptionView(
    string PlanCode,
    string PlanName,
    int VersionNumber,
    PlatformSubscriptionStatus Status,
    PlatformSubscriptionTerm Term,
    int Licences,
    int ActiveLicences,
    decimal PricePerLicence,
    decimal PeriodAmount,
    decimal NextPeriodNetAmount,
    string CurrencyCode,
    DateTimeOffset? TrialEndsUtc,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    bool CancelAtPeriodEnd,
    PlatformSubscriptionAccess Access,
    PlatformScheduledChangeItem? ScheduledChange,
    IReadOnlyDictionary<PlatformPlanLimitKind, int> Limits,
    IReadOnlyList<PlatformPlanFeature> Features);
