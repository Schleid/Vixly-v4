namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionAccess.cs
//
// PURPOSE:
// What a tenant may do in Operations given its subscription (D-S14).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Computed, never stored; Operations enforces it
// - PaymentRequired (Slice 6): tenant staff cannot sign in except the
//   owner and the billing contact, who see only the payment page
// - Never applies to the tenant's customers (owner, 2026-10-16)
// =========================================================
public enum PlatformSubscriptionAccess
{
    None = 0,
    ReadOnly = 1,
    Full = 2,
    PaymentRequired = 3
}
