using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyStock.cs
//
// PURPOSE:
// A supply's stock at one warehouse (Slice 5E, S-4 and S-8): what is on hand and the level to reorder at.
//
// RESPONSIBILITIES:
// - Keep the quantity on hand, moved only by movements
// - Keep the reorder level and say when the stock is low
//
// IMPORTANT NOTES:
// - One row per supply and warehouse; its row version guards two movements at once
// - On hand may go below zero when the company allows selling without stock (S-9)
// =========================================================
public sealed class OperationsSupplyStock : OperationsEntity
{
    private OperationsSupplyStock()
    {
    }

    private OperationsSupplyStock(Guid tenantId, Guid supplyId, Guid warehouseId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (SupplyId, WarehouseId) = (supplyId, warehouseId);
    }

    public Guid SupplyId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public int OnHand { get; private set; }

    public int? ReorderLevel { get; private set; }

    public bool IsLow => ReorderLevel is { } level && OnHand <= level;

    public static OperationsSupplyStock Create(Guid tenantId, Guid supplyId, Guid warehouseId, DateTimeOffset utcNow) => new(tenantId, supplyId, warehouseId, utcNow);

    public void Apply(OperationsSupplyMovement movement, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(movement);

        if (movement.SupplyId != SupplyId || movement.WarehouseId != WarehouseId)
        {
            throw new DomainException(OperationsSupplyStockErrors.LinesInvalid);
        }

        OnHand += movement.Quantity;
        MarkAsModified(utcNow);
    }

    public void Reorder(int? level, DateTimeOffset utcNow)
    {
        ReorderLevel = level is null or (>= 0 and <= OperationsSupplyStockLimits.MaxReorderLevel) ? level : throw new DomainException(OperationsSupplyStockErrors.QuantityInvalid);
        MarkAsModified(utcNow);
    }
}
