namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionChangeTiming.cs
//
// PURPOSE:
// When a requested change takes effect (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformSubscriptionChangeTiming
{
    Now = 0,
    AtPeriodEnd = 1
}
