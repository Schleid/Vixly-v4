namespace Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipment;

// =========================================================
// FILE: GetOperationsShipmentRequest.cs
//
// PURPOSE:
// One shipment for staff: where it goes, its customer, its pre-alerts and each parcel with where it stands (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsShipmentRequest
{
    public Guid ShipmentId { get; init; }
}
