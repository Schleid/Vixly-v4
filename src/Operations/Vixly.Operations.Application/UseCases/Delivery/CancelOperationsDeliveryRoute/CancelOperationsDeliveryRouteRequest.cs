namespace Vixly.Operations.Application.UseCases.Delivery.CancelOperationsDeliveryRoute;

// =========================================================
// FILE: CancelOperationsDeliveryRouteRequest.cs
//
// PURPOSE:
// Cancel a planned route: its shipments wait for another (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelOperationsDeliveryRouteRequest
{
    public Guid RouteId { get; init; }

    public string? Reason { get; init; }
}
