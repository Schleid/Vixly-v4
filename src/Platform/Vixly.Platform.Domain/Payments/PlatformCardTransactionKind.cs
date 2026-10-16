namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformCardTransactionKind.cs
//
// PURPOSE:
// Whether a card transaction takes money or gives it back.
//
// RESPONSIBILITIES:
// - Name the whether a card transaction takes money or gives it back
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformCardTransactionKind
{
    Charge = 1,
    Refund = 2
}
