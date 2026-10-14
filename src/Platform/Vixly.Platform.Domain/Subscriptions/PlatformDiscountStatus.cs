namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountStatus.cs
//
// PURPOSE:
// The state of a discount on a subscription.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - At most one Active and one PendingApproval per subscription
// =========================================================
public enum PlatformDiscountStatus
{
    PendingApproval = 0,
    Active = 1,
    Rejected = 2,
    Ended = 3
}
