using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.CancelMyOperationsShipment;

// =========================================================
// FILE: CancelMyOperationsShipmentRequest.cs
//
// PURPOSE:
// A customer cancels a shipment none of whose packages has arrived (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelMyOperationsShipmentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ShipmentId { get; init; }
}
