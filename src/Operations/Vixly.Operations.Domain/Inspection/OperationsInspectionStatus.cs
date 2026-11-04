namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionStatus.cs
//
// PURPOSE:
// Where a compliance inspection stands (Slice 3C).
//
// RESPONSIBILITIES:
// - Name the steps from the queue to the outcome
//
// IMPORTANT NOTES:
// - Stored; never renumber
// - AwaitingApproval: refused and to be disposed of, waiting for the Owner or an administrator (Q-C3)
// =========================================================
public enum OperationsInspectionStatus
{
    Waiting = 0,
    InProgress = 1,
    AwaitingApproval = 2,
    Done = 3
}
