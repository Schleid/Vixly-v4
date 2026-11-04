namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyBreak.cs
//
// PURPOSE:
// A lower unit price from a quantity (Slice 5E, S-2): "10 boxes or more: 1.80 each".
//
// RESPONSIBILITIES:
// - Carry the quantity from which it applies and its unit price
//
// IMPORTANT NOTES:
// - Part of an OperationsSupplyPrice; checked there
// =========================================================
public sealed class OperationsSupplyBreak
{
    private OperationsSupplyBreak()
    {
    }

    public OperationsSupplyBreak(int fromQuantity, decimal price)
    {
        FromQuantity = fromQuantity;
        Price = price;
    }

    public int FromQuantity { get; private set; }

    public decimal Price { get; private set; }
}
