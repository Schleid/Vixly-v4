namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformOneTimeChargeStatus.cs
//
// PURPOSE:
// The state of a one-time charge (D-S23).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Billed is set by Billing (Slice 4)
// =========================================================
public enum PlatformOneTimeChargeStatus
{
    Pending = 0,
    Billed = 1,
    Cancelled = 2
}
