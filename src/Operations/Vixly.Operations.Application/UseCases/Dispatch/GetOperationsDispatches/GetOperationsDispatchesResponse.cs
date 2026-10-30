using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatches;

// =========================================================
// FILE: GetOperationsDispatchesResponse.cs
//
// PURPOSE:
// The dispatch desk (Slice 6B, D-2): the dispatches of a person's warehouses, and what a new one can be for.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDispatchesResponse
{
    private GetOperationsDispatchesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsWarehouse> Destinations { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsDispatch> Dispatches { get; private init; } = Array.Empty<OperationsDispatch>();

    public IReadOnlyList<string> Services { get; private init; } = Array.Empty<string>();

    public IReadOnlyDictionary<Guid, int> FreeBundles { get; private init; } = new Dictionary<Guid, int>();

    public string? TimeZone { get; private init; }

    public static GetOperationsDispatchesResponse Success(IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyList<OperationsWarehouse> destinations, IReadOnlyList<OperationsDispatch> dispatches, IReadOnlyList<string> services, IReadOnlyDictionary<Guid, int> freeBundles, string? timeZone)
    {
        return new GetOperationsDispatchesResponse { Succeeded = true, Warehouses = warehouses, Destinations = destinations, Dispatches = dispatches, Services = services, FreeBundles = freeBundles, TimeZone = timeZone };
    }

    public static GetOperationsDispatchesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDispatchesResponse { Error = error };
    }
}
