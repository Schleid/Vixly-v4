namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyWarehousePrice.cs
//
// PURPOSE:
// The unit price of a supply at one warehouse, when it differs (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Carry the warehouse and its unit price
//
// IMPORTANT NOTES:
// - Part of an OperationsSupplyPrice; it replaces the company price there, quantity breaks included
// =========================================================
public sealed class OperationsSupplyWarehousePrice
{
    private OperationsSupplyWarehousePrice()
    {
    }

    public OperationsSupplyWarehousePrice(Guid warehouseId, decimal price)
    {
        WarehouseId = warehouseId;
        Price = price;
    }

    public Guid WarehouseId { get; private set; }

    public decimal Price { get; private set; }
}
