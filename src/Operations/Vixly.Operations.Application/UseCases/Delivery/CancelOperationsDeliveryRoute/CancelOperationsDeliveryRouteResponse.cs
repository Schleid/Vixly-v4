using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.CancelOperationsDeliveryRoute;

// =========================================================
// FILE: CancelOperationsDeliveryRouteResponse.cs
//
// PURPOSE:
// Cancel a planned route: its shipments wait for another (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelOperationsDeliveryRouteResponse
{
    private CancelOperationsDeliveryRouteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelOperationsDeliveryRouteResponse Success()
    {
        return new CancelOperationsDeliveryRouteResponse { Succeeded = true };
    }

    public static CancelOperationsDeliveryRouteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelOperationsDeliveryRouteResponse { Error = error };
    }
}
