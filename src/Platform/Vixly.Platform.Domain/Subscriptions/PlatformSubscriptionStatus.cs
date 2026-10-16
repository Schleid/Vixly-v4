namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionStatus.cs
//
// PURPOSE:
// The states of a tenant subscription (D-S6).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Cancellation is a flag on an Active subscription, not a status
// - Past due is set by Billing (Slice 4)
// - Blocked for non-payment is set and cleared only by Collections
//   and payment (Slice 6, D-S17); Suspended is for the account only
// =========================================================
public enum PlatformSubscriptionStatus
{
    Trialing = 0,
    PendingPayment = 1,
    Active = 2,
    PastDue = 3,
    Suspended = 4,
    Ended = 5,
    BlockedForNonPayment = 6
}
