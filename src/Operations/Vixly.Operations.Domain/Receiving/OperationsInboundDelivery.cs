using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsInboundDelivery.cs
//
// PURPOSE:
// One carrier delivery received at the dock: the packages one driver dropped (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Open a delivery, take scans while it is open, close it once
//
// IMPORTANT NOTES:
// - The piece count is counted from its parcels at closing, so scans never contend for this row
// - Closing issues the warehouse receipt the driver signs
// =========================================================
public sealed class OperationsInboundDelivery : OperationsEntity
{
    private OperationsInboundDelivery()
    {
    }

    private OperationsInboundDelivery(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid WarehouseId { get; private set; }

    public string? Carrier { get; private set; }

    public Guid OpenedByUserId { get; private set; }

    public OperationsDeliveryStatus Status { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public Guid? ReceiptId { get; private set; }

    // The reference of the form that opened it: the same form sent twice opens one delivery (ADR-0019).
    public string? OpenKey { get; private set; }

    public static OperationsInboundDelivery Open(Guid tenantId, Guid warehouseId, string? carrier, Guid openedBy, string openKey, DateTimeOffset utcNow) =>
        new(tenantId, utcNow)
        {
            OpenKey = openKey,
            WarehouseId = warehouseId,
            Carrier = OperationsReceivingText.Optional(carrier, OperationsReceivingLimits.CarrierMaxLength),
            OpenedByUserId = openedBy,
            Status = OperationsDeliveryStatus.Open
        };

    public void EnsureOpen()
    {
        if (Status != OperationsDeliveryStatus.Open)
        {
            throw new DomainException(OperationsReceivingErrors.DeliveryClosed);
        }
    }

    // Each parcel received changes the delivery row, so a scan and a close made at the same moment clash on its row version (review of the dock).
    public void TakePiece(DateTimeOffset utcNow)
    {
        EnsureOpen();
        MarkAsModified(utcNow);
    }

    public void Close(Guid receiptId, DateTimeOffset utcNow)
    {
        EnsureOpen();
        (Status, ClosedUtc, ReceiptId) = (OperationsDeliveryStatus.Closed, utcNow, receiptId);
        MarkAsModified(utcNow);
    }
}
