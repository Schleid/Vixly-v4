using Vixly.Operations.Application.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetMyOperationsDeliveryRoutes;

// =========================================================
// FILE: GetMyOperationsDeliveryRoutesResponse.cs
//
// PURPOSE:
// A driver's own routes, from yesterday on (Slice 7 step 3, R-12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsDeliveryRoutesResponse
{
    private GetMyOperationsDeliveryRoutesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsDeliveryRouteRow> Routes { get; private init; } = Array.Empty<OperationsDeliveryRouteRow>();

    public static GetMyOperationsDeliveryRoutesResponse Success(IReadOnlyList<OperationsDeliveryRouteRow> routes)
    {
        return new GetMyOperationsDeliveryRoutesResponse { Succeeded = true, Routes = routes };
    }

    public static GetMyOperationsDeliveryRoutesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsDeliveryRoutesResponse { Error = error };
    }
}
