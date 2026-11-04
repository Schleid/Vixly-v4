using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.PlanOperationsDeliveryRoute;

// =========================================================
// FILE: PlanOperationsDeliveryRouteResponse.cs
//
// PURPOSE:
// Plan a driver's route from a warehouse with shipments waiting for delivery (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PlanOperationsDeliveryRouteResponse
{
    private PlanOperationsDeliveryRouteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RouteId { get; private init; }

    public static PlanOperationsDeliveryRouteResponse Success(Guid routeId)
    {
        return new PlanOperationsDeliveryRouteResponse { Succeeded = true, RouteId = routeId };
    }

    public static PlanOperationsDeliveryRouteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlanOperationsDeliveryRouteResponse { Error = error };
    }
}
