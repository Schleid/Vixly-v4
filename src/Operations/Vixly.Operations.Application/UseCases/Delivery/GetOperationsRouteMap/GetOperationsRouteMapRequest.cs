namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsRouteMap;

// =========================================================
// FILE: GetOperationsRouteMapRequest.cs
//
// PURPOSE:
// The driver's latest place and today's way on the route page (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsRouteMapRequest
{
    public Guid RouteId { get; init; }
}
