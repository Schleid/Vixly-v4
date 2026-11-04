namespace Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment;

// =========================================================
// FILE: EstimateMyOperationsShipmentRequest.cs
//
// PURPOSE:
// The shipment estimate the customer sees on the portal while building or changing a shipment, and on their shipment (Slice 5D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EstimateMyOperationsShipmentRequest
{
    public string? SessionToken { get; init; }

    public Vixly.Operations.Application.Shipments.OperationsShipmentDraft? Draft { get; init; }

    public Guid? OriginWarehouseId { get; init; }

    // The value the customer expects to declare, for the insurance premium, while no parcel is in.
    public decimal? ExpectedValue { get; init; }

    // The customer's own shipment: its parcels are priced too.
    public Guid? ShipmentId { get; init; }
}
