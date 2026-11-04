namespace Vixly.Operations.Application.UseCases.Counter.EstimateOperationsCounterShipment;

// =========================================================
// FILE: EstimateOperationsCounterShipmentRequest.cs
//
// PURPOSE:
// The shipment estimate while the clerk fills in a new shipment, before any parcel (Slice 5D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EstimateOperationsCounterShipmentRequest
{
    public Guid CustomerId { get; init; }

    public Guid? OriginWarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    // "pickup" or "home".
    public string? Delivery { get; init; }

    public string? ServiceCode { get; init; }

    public bool Consolidate { get; init; }

    // None: no insurance.
    public Guid? InsurancePlanId { get; init; }

    // The value the customer expects to declare, for the insurance premium.
    public decimal? ExpectedValue { get; init; }
}
