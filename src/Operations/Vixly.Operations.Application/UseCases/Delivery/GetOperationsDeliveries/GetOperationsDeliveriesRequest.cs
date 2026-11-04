namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveries;

// =========================================================
// FILE: GetOperationsDeliveriesRequest.cs
//
// PURPOSE:
// The delivery desk of a warehouse: shipments waiting for delivery, its routes, parcels going back (Slice 7 step 3, R-7, R-14).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsDeliveriesRequest
{
    public Guid? WarehouseId { get; init; }
}
