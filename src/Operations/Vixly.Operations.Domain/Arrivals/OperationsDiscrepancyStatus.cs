namespace Vixly.Operations.Domain.Arrivals;

// =========================================================
// FILE: OperationsDiscrepancyStatus.cs
//
// PURPOSE:
// Where a discrepancy stands (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsDiscrepancyStatus
{
    Open = 1,

    // Scanned after all.
    Found = 2,

    // Confirmed missing: the customer is told.
    Lost = 3,

    // Sorted out otherwise (sent on, returned to its own warehouse), with a note.
    Resolved = 4
}
