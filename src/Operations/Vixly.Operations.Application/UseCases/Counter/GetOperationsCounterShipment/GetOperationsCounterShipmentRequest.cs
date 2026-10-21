namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterShipment;

// =========================================================
// FILE: GetOperationsCounterShipmentRequest.cs
//
// PURPOSE:
// Shows a shipment at the counter: its parcels against the count, and the amount so far or, once final, its amount (Slice 5C, C-7 and C-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCounterShipmentRequest
{
    public Guid ShipmentId { get; init; }
}
