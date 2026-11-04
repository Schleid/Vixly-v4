using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyCount.cs
//
// PURPOSE:
// A stock count of supplies at a warehouse (Slice 5E, S-7): the clerk enters what is on the shelf; closing turns each difference into a count correction.
//
// RESPONSIBILITIES:
// - Keep the warehouse, the supplies to count and the quantities entered
// - Open, save as it goes, close or cancel
//
// IMPORTANT NOTES:
// - One open count per warehouse (the use case checks)
// - The difference is taken against the stock on hand when the count closes: count when the counter is quiet
// =========================================================
public sealed class OperationsSupplyCount : OperationsEntity
{
    private readonly List<OperationsSupplyCountLine> _lines = new();

    private OperationsSupplyCount()
    {
    }

    private OperationsSupplyCount(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid WarehouseId { get; private set; }

    public IReadOnlyList<OperationsSupplyCountLine> Lines => _lines;

    public Guid OpenedByUserId { get; private set; }

    public Guid? ClosedByUserId { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public bool Cancelled { get; private set; }

    public bool IsOpen => ClosedUtc is null;

    public static OperationsSupplyCount Open(Guid tenantId, Guid warehouseId, IReadOnlyList<Guid> supplyIds, Guid byUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(supplyIds);

        if (warehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsSupplyStockErrors.WarehouseInvalid);
        }

        if (supplyIds.Count is 0 or > OperationsSupplyLimits.MaxSupplies || supplyIds.Distinct().Count() != supplyIds.Count)
        {
            throw new DomainException(OperationsSupplyStockErrors.LinesInvalid);
        }

        OperationsSupplyCount count = new(tenantId, utcNow) { WarehouseId = warehouseId, OpenedByUserId = byUserId };
        count._lines.AddRange(supplyIds.Select(id => new OperationsSupplyCountLine(id)));
        return count;
    }

    // The quantities entered so far; a supply not in the count is refused.
    public void Enter(IReadOnlyDictionary<Guid, int?> counted, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(counted);
        EnsureOpen();

        foreach ((Guid supplyId, int? quantity) in counted)
        {
            if (_lines.FirstOrDefault(line => line.SupplyId == supplyId) is not { } line || quantity is < 0 or > OperationsSupplyStockLimits.MaxMovementQuantity)
            {
                throw new DomainException(OperationsSupplyStockErrors.QuantityInvalid);
            }

            line.Count(quantity);
        }

        MarkAsModified(utcNow);
    }

    public void Close(Guid byUserId, bool cancel, DateTimeOffset utcNow)
    {
        EnsureOpen();
        (ClosedByUserId, ClosedUtc, Cancelled) = (byUserId, utcNow, cancel);
        MarkAsModified(utcNow);
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new DomainException(OperationsSupplyStockErrors.CountClosed);
        }
    }
}
