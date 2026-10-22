namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsPaymentState.cs
//
// PURPOSE:
// Where an invoice stands with its payments (Slice 5F, C-7).
//
// RESPONSIBILITIES:
// - Name the states shown on the desk, the counter and the cashier
//
// IMPORTANT NOTES:
// - Not stored: read from the invoice's amounts
// =========================================================
public enum OperationsPaymentState
{
    Unpaid = 1,
    AwaitingConfirmation = 2,
    Paid = 3,
    Voided = 4,

    // Slice 5F step 2b: billed to the customer's credit account (released, still owed), or waiting for the credit department.
    OnAccount = 5,

    AwaitingCredit = 6
}
