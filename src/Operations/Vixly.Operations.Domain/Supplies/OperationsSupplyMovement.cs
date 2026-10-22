using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyMovement.cs
//
// PURPOSE:
// One change to a supply's stock at a warehouse (Slice 5E, S-4): received, sold, sale cancelled, adjusted, transferred, counted.
//
// RESPONSIBILITIES:
// - Record who, when, how many (positive in, negative out) and why
// - Keep the cost of stock received, the supplier and the reference
//
// IMPORTANT NOTES:
// - Append only: a movement is never changed or removed; a mistake is corrected by another movement
// - Link: the form, transfer, count or sale it belongs to (the same form sent twice moves the stock once, ADR-0019)
// =========================================================
public sealed class OperationsSupplyMovement : OperationsEntity
{
    private OperationsSupplyMovement()
    {
    }

    private OperationsSupplyMovement(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid SupplyId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public OperationsSupplyMovementKind Kind { get; private set; }

    // In: positive; out: negative.
    public int Quantity { get; private set; }

    public decimal? UnitCost { get; private set; }

    public OperationsSupplyAdjustReason? Reason { get; private set; }

    public string? Note { get; private set; }

    public string? Supplier { get; private set; }

    public string? Reference { get; private set; }

    public Guid? LinkId { get; private set; }

    public Guid? ByUserId { get; private set; }

    public static OperationsSupplyMovement Received(Guid tenantId, Guid supplyId, Guid warehouseId, int quantity, decimal? unitCost, string? supplier, string? reference,
        Guid linkId, Guid byUserId, DateTimeOffset utcNow)
    {
        OperationsSupplyMovement movement = Create(tenantId, supplyId, warehouseId, OperationsSupplyMovementKind.Received, Positive(quantity), linkId, byUserId, utcNow);
        movement.UnitCost = unitCost is { } cost ? OperationsChargeLimits.UnitPrice(cost, OperationsSupplyStockErrors.CostInvalid) : null;
        movement.Supplier = OperationsChargeLimits.Text(supplier, OperationsSupplyStockLimits.SupplierMaxLength, OperationsSupplyErrors.TextInvalid);
        movement.Reference = OperationsChargeLimits.Text(reference, OperationsSupplyStockLimits.ReferenceMaxLength, OperationsSupplyErrors.TextInvalid);
        return movement;
    }

    // By hand: more or fewer, with a reason; a correction or "other" also needs a note.
    public static OperationsSupplyMovement Adjusted(Guid tenantId, Guid supplyId, Guid warehouseId, int quantity, OperationsSupplyAdjustReason reason, string? note,
        Guid linkId, Guid byUserId, DateTimeOffset utcNow)
    {
        string? words = OperationsChargeLimits.Text(note, OperationsSupplyStockLimits.NoteMaxLength, OperationsSupplyErrors.TextInvalid);

        if (!Enum.IsDefined(reason) || (reason is OperationsSupplyAdjustReason.Correction or OperationsSupplyAdjustReason.Other && words is null))
        {
            throw new DomainException(OperationsSupplyStockErrors.ReasonRequired);
        }

        OperationsSupplyMovement movement = Create(tenantId, supplyId, warehouseId, OperationsSupplyMovementKind.Adjusted, NotZero(quantity), linkId, byUserId, utcNow);
        (movement.Reason, movement.Note) = (reason, words);
        return movement;
    }

    public static OperationsSupplyMovement Moved(Guid tenantId, Guid supplyId, Guid warehouseId, OperationsSupplyMovementKind kind, int quantity, Guid linkId, Guid? byUserId,
        string? reference, DateTimeOffset utcNow)
    {
        int signed = kind switch
        {
            OperationsSupplyMovementKind.Sold or OperationsSupplyMovementKind.TransferOut => -Positive(quantity),
            OperationsSupplyMovementKind.SaleCancelled or OperationsSupplyMovementKind.TransferIn => Positive(quantity),
            OperationsSupplyMovementKind.Counted => NotZero(quantity),
            _ => throw new DomainException(OperationsSupplyStockErrors.QuantityInvalid)
        };

        OperationsSupplyMovement movement = Create(tenantId, supplyId, warehouseId, kind, signed, linkId, byUserId, utcNow);
        movement.Reference = OperationsChargeLimits.Text(reference, OperationsSupplyStockLimits.ReferenceMaxLength, OperationsSupplyErrors.TextInvalid);
        return movement;
    }

    private static OperationsSupplyMovement Create(Guid tenantId, Guid supplyId, Guid warehouseId, OperationsSupplyMovementKind kind, int quantity, Guid linkId, Guid? byUserId,
        DateTimeOffset utcNow)
    {
        if (supplyId == Guid.Empty || warehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsSupplyStockErrors.LinesInvalid);
        }

        return new OperationsSupplyMovement(tenantId, utcNow)
        {
            SupplyId = supplyId, WarehouseId = warehouseId, Kind = kind, Quantity = quantity, LinkId = linkId == Guid.Empty ? null : linkId, ByUserId = byUserId
        };
    }

    private static int Positive(int quantity) =>
        quantity is > 0 and <= OperationsSupplyStockLimits.MaxMovementQuantity ? quantity : throw new DomainException(OperationsSupplyStockErrors.QuantityInvalid);

    private static int NotZero(int quantity) =>
        quantity != 0 && Math.Abs(quantity) <= OperationsSupplyStockLimits.MaxMovementQuantity ? quantity : throw new DomainException(OperationsSupplyStockErrors.QuantityInvalid);
}
