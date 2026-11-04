namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyLine.cs
//
// PURPOSE:
// A supply and a quantity: a line of a transfer (Slice 5E, S-6).
//
// RESPONSIBILITIES:
// - Carry the supply and how many
//
// IMPORTANT NOTES:
// - Part of a transfer; checked there
// =========================================================
public sealed class OperationsSupplyLine
{
    private OperationsSupplyLine()
    {
    }

    public OperationsSupplyLine(Guid supplyId, int quantity)
    {
        SupplyId = supplyId;
        Quantity = quantity;
    }

    public Guid SupplyId { get; private set; }

    public int Quantity { get; private set; }
}
