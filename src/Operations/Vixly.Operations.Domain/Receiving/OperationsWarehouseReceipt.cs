using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsWarehouseReceipt.cs
//
// PURPOSE:
// The warehouse receipt of one carrier delivery: who brought how many packages, when, and how many were damaged (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Issue the receipt when the delivery closes
//
// IMPORTANT NOTES:
// - Never changed once written (the application login has no UPDATE or DELETE)
// - Damage seen at the dock is on the receipt before the driver leaves, so a carrier claim stays possible
// =========================================================
public sealed class OperationsWarehouseReceipt : OperationsEntity
{
    private OperationsWarehouseReceipt()
    {
        Number = string.Empty;
    }

    private OperationsWarehouseReceipt(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = string.Empty;
    }

    public string Number { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid DeliveryId { get; private set; }

    public string? Carrier { get; private set; }

    public Guid ReceivedByUserId { get; private set; }

    public DateTimeOffset OpenedUtc { get; private set; }

    public int PieceCount { get; private set; }

    public int DamagedCount { get; private set; }

    public static OperationsWarehouseReceipt Issue(Guid tenantId, string number, OperationsInboundDelivery delivery, Guid receivedBy, int pieceCount, int damagedCount, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentNullException.ThrowIfNull(delivery);

        return new(tenantId, utcNow)
        {
            Number = number,
            WarehouseId = delivery.WarehouseId,
            DeliveryId = delivery.Id,
            Carrier = delivery.Carrier,
            ReceivedByUserId = receivedBy,
            OpenedUtc = delivery.CreatedUtc,
            PieceCount = Math.Max(pieceCount, 0),
            DamagedCount = Math.Clamp(damagedCount, 0, Math.Max(pieceCount, 0))
        };
    }
}
