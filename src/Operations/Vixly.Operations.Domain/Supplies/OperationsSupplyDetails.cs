namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyDetails.cs
//
// PURPOSE:
// What describes a shipping supply, as staff enter it (Slice 5E, S-1).
//
// RESPONSIBILITIES:
// - Carry the item code, the names in three languages, the unit, the category and whether it is taxed
//
// IMPORTANT NOTES:
// - Checked by OperationsSupply; the English name is required, French and Spanish fall back to it
// =========================================================
public sealed record OperationsSupplyDetails(
    string? Code,
    string? NameEn,
    string? NameFr,
    string? NameEs,
    string? Unit,
    OperationsSupplyCategory Category,
    bool Taxed);
