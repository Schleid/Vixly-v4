namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountEndReason.cs
//
// PURPOSE:
// Why a discount stopped.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformDiscountEndReason
{
    Completed = 1,
    Replaced = 2,
    EndedByStaff = 3,
    SubscriptionEnded = 4
}
