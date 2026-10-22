namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyLabel.cs
//
// PURPOSE:
// What a shelf label of a supply shows (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Carry the name, the item code (printed as a barcode) and the price today
//
// IMPORTANT NOTES:
// - Price null: the supply has no price yet
// =========================================================
public sealed record OperationsSupplyLabel(string Name, string Code, decimal? Price, string CurrencyCode);
