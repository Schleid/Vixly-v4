namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountDuration.cs
//
// PURPOSE:
// How long a discount lasts (D-S19).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Periods are billing periods: a month on monthly, a year on annual
// =========================================================
public enum PlatformDiscountDuration
{
    Periods = 1,
    UntilDate = 2,
    NoEnd = 3
}
