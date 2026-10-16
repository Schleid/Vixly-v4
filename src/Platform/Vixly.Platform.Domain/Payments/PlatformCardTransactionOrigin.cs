namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformCardTransactionOrigin.cs
//
// PURPOSE:
// What started a card transaction.
//
// RESPONSIBILITIES:
// - Name the what started a card transaction
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformCardTransactionOrigin
{
    PayLink = 1,
    Registration = 2,
    StaffCharge = 3,
    Automatic = 4,
    Refund = 5,
    ExcessRefund = 6,
    Retry = 7
}
