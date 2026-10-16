namespace Vixly.Contracts.Platform.Internal.Subscription;

// =========================================================
// FILE: PlatformSubscriptionInfo.cs
//
// PURPOSE:
// The tenant's subscription (call 19).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Term: monthly or annual
// - No content (204) when the tenant has no subscription
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformSubscriptionInfo(
    string PlanCode,
    string PlanName,
    int VersionNumber,
    string Status,
    string Term,
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
    string Access,
    PlatformScheduledChangeInfo? ScheduledChange,
    IReadOnlyDictionary<string, int> Limits,
    IReadOnlyList<string> Features);
