using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.ChangeMyOperationsShipment;

// =========================================================
// FILE: ChangeMyOperationsShipmentRequest.cs
//
// PURPOSE:
// A customer changes where an open shipment goes, who receives it or its options (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsShipmentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ShipmentId { get; init; }

    public OperationsShipmentDraft Instructions { get; init; } = null!;
}
