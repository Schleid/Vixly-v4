namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsHoldAnswerStatus.cs
//
// PURPOSE:
// Where a customer's answer to a hold stands (Slice 3E).
//
// RESPONSIBILITIES:
// - Name the steps from sent to reviewed
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsHoldAnswerStatus
{
    Open = 0,
    Accepted = 1,
    Declined = 2,

    // The customer sent a newer answer for the same hold.
    Replaced = 3
}
