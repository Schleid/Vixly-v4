namespace Vixly.Operations.Application.UseCases.Shipments.ChangeOperationsShipmentService;

// =========================================================
// FILE: ChangeOperationsShipmentServiceRequest.cs
//
// PURPOSE:
// Staff change the service of a shipment before it leaves (Slice 5A; owner, Q-D1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsShipmentServiceRequest
{
    public Guid ShipmentId { get; init; }

    public string? ServiceCode { get; init; }
}
