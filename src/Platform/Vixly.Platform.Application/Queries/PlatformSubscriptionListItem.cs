using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformSubscriptionListItem.cs
//
// PURPOSE:
// One row of the subscriptions list.
//
// RESPONSIBILITIES:
// - Carry what the list shows about a subscription and its tenant
//
// IMPORTANT NOTES:
// - Amount is the current period amount before tax
// =========================================================
public sealed record PlatformSubscriptionListItem(
    Guid PlatformSubscriptionId,
    Guid PlatformTenantId,
    string TenantCode,
    string TenantName,
    string PlanCode,
    string PlanName,
    int VersionNumber,
    PlatformSubscriptionStatus Status,
    PlatformSubscriptionTerm Term,
    int Licences,
    decimal PeriodAmount,
    string CurrencyCode,
    DateTimeOffset? PeriodEndUtc,
    DateTimeOffset? TrialEndsUtc,
    bool CancelAtPeriodEnd,
    bool HasScheduledChange,
    bool HasDiscount);
