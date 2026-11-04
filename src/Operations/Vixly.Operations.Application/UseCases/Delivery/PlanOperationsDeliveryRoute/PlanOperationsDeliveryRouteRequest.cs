namespace Vixly.Operations.Application.UseCases.Delivery.PlanOperationsDeliveryRoute;

// =========================================================
// FILE: PlanOperationsDeliveryRouteRequest.cs
//
// PURPOSE:
// Plan a driver's route from a warehouse with shipments waiting for delivery (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PlanOperationsDeliveryRouteRequest
{
    public Guid WarehouseId { get; init; }

    public DateOnly ServiceDate { get; init; }

    public Guid? DriverUserId { get; init; }

    public string? DriverName { get; init; }

    public IReadOnlyList<Guid> ShipmentIds { get; init; } = Array.Empty<Guid>();

    public Guid? RequestKey { get; init; }
}
