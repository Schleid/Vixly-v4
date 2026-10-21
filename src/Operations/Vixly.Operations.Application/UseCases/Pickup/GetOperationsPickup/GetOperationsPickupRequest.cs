namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickup;

// =========================================================
// FILE: GetOperationsPickupRequest.cs
//
// PURPOSE:
// A shipment at the pickup counter: who may collect, what is ready, what is owed, the proof asked (Slice 7 step 2, R-4 to R-6, R-9, R-10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPickupRequest
{
    public Guid ShipmentId { get; init; }

    public Guid WarehouseId { get; init; }
}
