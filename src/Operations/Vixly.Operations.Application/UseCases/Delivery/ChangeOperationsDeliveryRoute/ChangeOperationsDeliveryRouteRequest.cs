namespace Vixly.Operations.Application.UseCases.Delivery.ChangeOperationsDeliveryRoute;

// =========================================================
// FILE: ChangeOperationsDeliveryRouteRequest.cs
//
// PURPOSE:
// Change a planned route: add shipments waiting, take a stop off, move a stop up or down (Slice 7 step 3, R-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsDeliveryRouteRequest
{
    public Guid RouteId { get; init; }

    public IReadOnlyList<Guid> AddShipmentIds { get; init; } = Array.Empty<Guid>();

    public Guid? RemoveStopId { get; init; }

    public Guid? MoveStopId { get; init; }

    public bool Up { get; init; }
}
