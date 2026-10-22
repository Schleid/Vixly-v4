namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveryRoute;

// =========================================================
// FILE: GetOperationsDeliveryRouteRequest.cs
//
// PURPOSE:
// A delivery route: its stops, what to collect, how each went; the shipments that may join it while planned (Slice 7 step 3, R-7, R-12, R-13, R-15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsDeliveryRouteRequest
{
    public Guid RouteId { get; init; }
}
