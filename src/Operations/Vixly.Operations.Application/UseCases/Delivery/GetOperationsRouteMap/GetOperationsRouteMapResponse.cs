using Vixly.Operations.Application.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsRouteMap;

// =========================================================
// FILE: GetOperationsRouteMapResponse.cs
//
// PURPOSE:
// The driver's latest place and today's way on the route page (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsRouteMapResponse
{
    private GetOperationsRouteMapResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsRouteMap? Map { get; private init; }

    public static GetOperationsRouteMapResponse Success(OperationsRouteMap? map)
    {
        return new GetOperationsRouteMapResponse { Succeeded = true, Map = map };
    }

    public static GetOperationsRouteMapResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsRouteMapResponse { Error = error };
    }
}
