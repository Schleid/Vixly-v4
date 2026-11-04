namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBillPaymentMethod.cs
//
// PURPOSE:
// How the company paid a bill (Slice 8).
//
// RESPONSIBILITIES:
// - Name each method
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Vixly never sends money: a person records a payment made elsewhere
// =========================================================
public enum OperationsBillPaymentMethod
{
    BankTransfer = 1,
    Cheque = 2,
    Card = 3,
    Cash = 4,
    Zelle = 5,
    Other = 9
}
