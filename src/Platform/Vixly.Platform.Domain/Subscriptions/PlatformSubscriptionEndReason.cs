namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionEndReason.cs
//
// PURPOSE:
// Why a subscription ended.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformSubscriptionEndReason
{
    Cancelled = 1,
    EndedByStaff = 2,
    TenantClosed = 3,
    Replaced = 4
}
