using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.SettleOperationsDeliveryRoute;

// =========================================================
// FILE: SettleOperationsDeliveryRouteResponse.cs
//
// PURPOSE:
// Settle a route back from its round: the cash counted, once every invoice it collected for is paid in at the cashier (Slice 7 step 3, R-13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SettleOperationsDeliveryRouteResponse
{
    private SettleOperationsDeliveryRouteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SettleOperationsDeliveryRouteResponse Success()
    {
        return new SettleOperationsDeliveryRouteResponse { Succeeded = true };
    }

    public static SettleOperationsDeliveryRouteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SettleOperationsDeliveryRouteResponse { Error = error };
    }
}
