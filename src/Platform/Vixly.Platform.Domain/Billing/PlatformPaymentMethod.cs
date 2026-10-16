namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformPaymentMethod.cs
//
// PURPOSE:
// How a payment was received (D-B7).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Card is recorded only by the payment provider (Slice 5)
// =========================================================
public enum PlatformPaymentMethod
{
    BankTransfer = 1,
    Cash = 2,
    Cheque = 3,
    Zelle = 4,
    Other = 5,
    Card = 6
}
