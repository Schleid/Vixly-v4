namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsItemUnit.cs
//
// PURPOSE:
// The unit an item is counted in on customs papers (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Name the units
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsItemUnit
{
    Pieces = 1,
    Pairs = 2,
    Kilograms = 3,
    Litres = 4,
    Sets = 5
}
