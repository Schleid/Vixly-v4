namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyCountLine.cs
//
// PURPOSE:
// One supply in a stock count: what the clerk found on the shelf (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Carry the supply and the quantity counted, once entered
//
// IMPORTANT NOTES:
// - Part of a count; null: not counted yet (left as it is when the count closes)
// =========================================================
public sealed class OperationsSupplyCountLine
{
    private OperationsSupplyCountLine()
    {
    }

    public OperationsSupplyCountLine(Guid supplyId)
    {
        SupplyId = supplyId;
    }

    public Guid SupplyId { get; private set; }

    public int? Counted { get; private set; }

    internal void Count(int? counted) => Counted = counted;
}
