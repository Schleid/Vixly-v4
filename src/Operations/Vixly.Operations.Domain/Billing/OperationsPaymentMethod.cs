namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsPaymentMethod.cs
//
// PURPOSE:
// How a customer pays (Slice 5F, P-5 and C-1).
//
// RESPONSIBILITIES:
// - Name the methods
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Card comes with Stripe (step 4) and Account with credit accounts (step 2b)
// =========================================================
public enum OperationsPaymentMethod
{
    Cash = 1,
    Zelle = 2,
    Cheque = 3,
    BankTransfer = 4,

    // The company's own named methods (MonCash, Natcash...).
    Other1 = 5,
    Other2 = 6,
    Other3 = 7,

    // Slice 5F step 4: a card through the company's Stripe account, recorded only once Stripe says it is paid.
    Card = 8
}
