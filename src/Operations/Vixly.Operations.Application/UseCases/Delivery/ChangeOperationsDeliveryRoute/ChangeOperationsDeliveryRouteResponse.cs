using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.ChangeOperationsDeliveryRoute;

// =========================================================
// FILE: ChangeOperationsDeliveryRouteResponse.cs
//
// PURPOSE:
// Change a planned route: add shipments waiting, take a stop off, move a stop up or down (Slice 7 step 3, R-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsDeliveryRouteResponse
{
    private ChangeOperationsDeliveryRouteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsDeliveryRouteResponse Success()
    {
        return new ChangeOperationsDeliveryRouteResponse { Succeeded = true };
    }

    public static ChangeOperationsDeliveryRouteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsDeliveryRouteResponse { Error = error };
    }
}
