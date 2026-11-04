namespace Vixly.Operations.Domain.Arrivals;

// =========================================================
// FILE: OperationsDiscrepancyKind.cs
//
// PURPOSE:
// What did not match at a destination (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsDiscrepancyKind
{
    MissingBundle = 1,
    MissingParcel = 2,
    ExtraBundle = 3,
    ExtraParcel = 4
}
