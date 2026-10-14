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
// =========================================================
public enum PlatformSubscriptionAccess
{
    None = 0,
    ReadOnly = 1,
    Full = 2
}
