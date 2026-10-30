using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyTransfer.cs
//
// PURPOSE:
// Supplies sent from one of the company's warehouses to another (Slice 5E, S-6).
//
// RESPONSIBILITIES:
// - Keep the two warehouses, the lines, who sent and who received
// - Say whether the supplies are still in transit
//
// IMPORTANT NOTES:
// - Sent: stock leaves the first warehouse (transfer out); received: it arrives at the second (transfer in), as sent
// - Each supply once per transfer
// =========================================================
public sealed class OperationsSupplyTransfer : OperationsEntity
{
    private readonly List<OperationsSupplyLine> _lines = new();

    private OperationsSupplyTransfer()
    {
    }

    private OperationsSupplyTransfer(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid FromWarehouseId { get; private set; }

    public Guid ToWarehouseId { get; private set; }

    public IReadOnlyList<OperationsSupplyLine> Lines => _lines;

    public string? Note { get; private set; }

    public Guid SentByUserId { get; private set; }

    public Guid? ReceivedByUserId { get; private set; }

    public DateTimeOffset? ReceivedUtc { get; private set; }

    public bool InTransit => ReceivedUtc is null;

    public static OperationsSupplyTransfer Send(Guid tenantId, Guid fromWarehouseId, Guid toWarehouseId, IReadOnlyList<OperationsSupplyLine> lines, string? note, Guid byUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (fromWarehouseId == Guid.Empty || toWarehouseId == Guid.Empty || fromWarehouseId == toWarehouseId)
        {
            throw new DomainException(OperationsSupplyStockErrors.WarehouseInvalid);
        }

        if (lines.Count is 0 or > OperationsSupplyStockLimits.MaxLines || lines.Select(line => line.SupplyId).Distinct().Count() != lines.Count
            || lines.Any(line => line.SupplyId == Guid.Empty || line.Quantity is <= 0 or > OperationsSupplyStockLimits.MaxMovementQuantity))
        {
            throw new DomainException(OperationsSupplyStockErrors.LinesInvalid);
        }

        OperationsSupplyTransfer transfer = new(tenantId, utcNow)
        {
            FromWarehouseId = fromWarehouseId, ToWarehouseId = toWarehouseId, SentByUserId = byUserId,
            Note = OperationsChargeLimits.Text(note, OperationsSupplyStockLimits.NoteMaxLength, OperationsSupplyErrors.TextInvalid)
        };
        transfer._lines.AddRange(lines);
        return transfer;
    }

    public void Receive(Guid byUserId, DateTimeOffset utcNow)
    {
        if (!InTransit)
        {
            throw new DomainException(OperationsSupplyStockErrors.TransferDone);
        }

        (ReceivedByUserId, ReceivedUtc) = (byUserId, utcNow);
        MarkAsModified(utcNow);
    }
}
