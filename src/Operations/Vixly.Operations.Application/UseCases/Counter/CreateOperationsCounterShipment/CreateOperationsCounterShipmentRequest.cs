using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Counter.CreateOperationsCounterShipment;

// =========================================================
// FILE: CreateOperationsCounterShipmentRequest.cs
//
// PURPOSE:
// Creates a customer's shipment at the counter, with its parcel count (Slice 5C, C-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsCounterShipmentRequest
{
    public Guid CustomerId { get; init; }

    public Guid WarehouseId { get; init; }

    public required OperationsShipmentDraft Draft { get; init; }

    public int ParcelCount { get; init; }

    public string? RequestKey { get; init; }
}
