using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipment;

// =========================================================
// FILE: GetMyOperationsShipmentRequest.cs
//
// PURPOSE:
// A customer's shipment with its announced and arrived packages (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsShipmentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ShipmentId { get; init; }
}
