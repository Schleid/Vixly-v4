namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountKind.cs
//
// PURPOSE:
// How a discount reduces the price (D-S18).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformDiscountKind
{
    Percent = 1,
    FixedAmount = 2
}
