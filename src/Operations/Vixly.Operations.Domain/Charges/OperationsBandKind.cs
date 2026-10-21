namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsBandKind.cs
//
// PURPOSE:
// How one band of a price list charges (owner, 2026-10-19: "it's not to Vixly to decide").
//
// RESPONSIBILITIES:
// - Name the three kinds
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsBandKind
{
    // One amount for any billable weight (or size) in the band.
    Flat = 1,

    // The amount for each lb, kg, ft³ or m³ of the whole billable weight (or size).
    PerUnit = 2,

    // A base, plus the amount for each unit above the band's start.
    BasePlusPerUnit = 3
}
