namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountSource.cs
//
// PURPOSE:
// Where a discount came from.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformDiscountSource
{
    Staff = 1,
    Promotion = 2,
    Offer = 3
}
