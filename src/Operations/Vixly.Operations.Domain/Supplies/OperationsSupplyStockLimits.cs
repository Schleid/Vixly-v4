namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyStockLimits.cs
//
// PURPOSE:
// The sizes the supplies stock accepts (Slice 5E step 2).
//
// RESPONSIBILITIES:
// - Bound quantities, costs, texts and lines
//
// IMPORTANT NOTES:
// - One movement moves at most 99,999 units
// =========================================================
public static class OperationsSupplyStockLimits
{
    public const int MaxMovementQuantity = 99_999;

    public const int MaxReorderLevel = 99_999;

    public const int MaxLines = 50;

    public const int NoteMaxLength = 300;

    public const int SupplierMaxLength = 80;

    public const int ReferenceMaxLength = 60;
}
