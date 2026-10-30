namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardChargeStatus.cs
//
// PURPOSE:
// Where a card payment stands (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Name each state
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Paid only on Stripe's word (its webhook or a check against Stripe), never on the browser's
// =========================================================
public enum OperationsCardChargeStatus
{
    Started = 1,
    Paid = 2,
    Cancelled = 3,

    // Stripe took the money but the invoice no longer owed it (changed meanwhile): staff refund it.
    PaidNotApplied = 4
}
