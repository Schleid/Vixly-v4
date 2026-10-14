namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformPromotionStatus.cs
//
// PURPOSE:
// The state of a promotion (D-S20, D-S26).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformPromotionStatus
{
    Draft = 0,
    PendingApproval = 1,
    Active = 2,
    Rejected = 3,
    Paused = 4,
    Ended = 5
}
