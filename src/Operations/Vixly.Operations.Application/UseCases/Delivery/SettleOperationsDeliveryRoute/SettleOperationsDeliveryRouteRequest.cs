namespace Vixly.Operations.Application.UseCases.Delivery.SettleOperationsDeliveryRoute;

// =========================================================
// FILE: SettleOperationsDeliveryRouteRequest.cs
//
// PURPOSE:
// Settle a route back from its round: the cash counted, once every invoice it collected for is paid in at the cashier (Slice 7 step 3, R-13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SettleOperationsDeliveryRouteRequest
{
    public Guid RouteId { get; init; }

    public decimal HandedIn { get; init; }

    public string? Note { get; init; }
}
