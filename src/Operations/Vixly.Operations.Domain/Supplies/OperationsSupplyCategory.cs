namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyCategory.cs
//
// PURPOSE:
// The group a shipping supply is listed under (Slice 5E, S-1).
//
// RESPONSIBILITIES:
// - Name the lists the counter and the catalogue filter by
//
// IMPORTANT NOTES:
// - Stored as a number; text comes from OperationsText (ui.supplies.category.*)
// =========================================================
public enum OperationsSupplyCategory
{
    Boxes = 1,
    Tape = 2,
    Padding = 3,
    Bags = 4,
    Other = 5
}
