namespace Vixly.Operations.Application.UseCases.Delivery.StartOperationsDeliveryRoute;

// =========================================================
// FILE: StartOperationsDeliveryRouteRequest.cs
//
// PURPOSE:
// Send a planned route out: its parcels are out for delivery and the customers are told (Slice 7 step 3, R-7, R-15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartOperationsDeliveryRouteRequest
{
    public Guid RouteId { get; init; }

    public string? PortalSlug { get; init; }
}
