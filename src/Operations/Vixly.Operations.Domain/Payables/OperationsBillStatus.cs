namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBillStatus.cs
//
// PURPOSE:
// Where a bill stands (Slice 8).
//
// RESPONSIBILITIES:
// - Name each state
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Only an approved bill counts in the costs and can be paid
// =========================================================
public enum OperationsBillStatus
{
    // Entered, waiting for someone else to approve it.
    Entered = 1,

    Approved = 2,

    // Sent back with a reason; it can be corrected and approved later.
    Rejected = 3,

    // Approved and paid in full.
    Paid = 4,

    // Entered by mistake; kept for the record.
    Voided = 5
}
