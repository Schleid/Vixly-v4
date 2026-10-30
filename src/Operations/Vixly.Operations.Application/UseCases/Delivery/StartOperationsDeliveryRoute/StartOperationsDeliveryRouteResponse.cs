using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.StartOperationsDeliveryRoute;

// =========================================================
// FILE: StartOperationsDeliveryRouteResponse.cs
//
// PURPOSE:
// Send a planned route out: its parcels are out for delivery and the customers are told (Slice 7 step 3, R-7, R-15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartOperationsDeliveryRouteResponse
{
    private StartOperationsDeliveryRouteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static StartOperationsDeliveryRouteResponse Success()
    {
        return new StartOperationsDeliveryRouteResponse { Succeeded = true };
    }

    public static StartOperationsDeliveryRouteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartOperationsDeliveryRouteResponse { Error = error };
    }
}
