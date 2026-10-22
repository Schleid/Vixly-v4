namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsQuotePiece.cs
//
// PURPOSE:
// One piece of a quote, as it was priced (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Keep the weight, size, value, item type and contents of the piece
//
// IMPORTANT NOTES:
// - Stored inside the quote (JSON); kg and cm (D-O8)
// =========================================================
public sealed class OperationsQuotePiece
{
    private OperationsQuotePiece()
    {
    }

    public OperationsQuotePiece(decimal weightKg, decimal lengthCm, decimal widthCm, decimal heightCm, decimal declaredValue, string? itemName, string? contents, Guid? itemTypeId = null)
    {
        (WeightKg, LengthCm, WidthCm, HeightCm, DeclaredValue, ItemName, Contents, ItemTypeId) = (weightKg, lengthCm, widthCm, heightCm, declaredValue, itemName, contents, itemTypeId);
    }

    // Slice 4B: the company's item type, when the piece has one.
    public Guid? ItemTypeId { get; private set; }

    public decimal WeightKg { get; private set; }

    public decimal LengthCm { get; private set; }

    public decimal WidthCm { get; private set; }

    public decimal HeightCm { get; private set; }

    public decimal DeclaredValue { get; private set; }

    public string? ItemName { get; private set; }

    public string? Contents { get; private set; }
}
