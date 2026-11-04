namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsQuoteLine.cs
//
// PURPOSE:
// One line of a quote, as the engine priced it, or the preferred price (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Keep the kind, name, piece, quantity, price and amount
//
// IMPORTANT NOTES:
// - Stored inside the quote (JSON)
// =========================================================
public sealed class OperationsQuoteLine
{
    private OperationsQuoteLine()
    {
    }

    public OperationsQuoteLine(OperationsChargeLineKind kind, string? label, int? piece, decimal quantity, decimal? basis, decimal unitPrice, decimal amount, bool isGovernmentFee)
    {
        (Kind, Label, Piece, Quantity, Basis, UnitPrice, Amount, IsGovernmentFee) = (kind, label, piece, quantity, basis, unitPrice, amount, isGovernmentFee);
    }

    public OperationsChargeLineKind Kind { get; private set; }

    public string? Label { get; private set; }

    public int? Piece { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal? Basis { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Amount { get; private set; }

    public bool IsGovernmentFee { get; private set; }
}
