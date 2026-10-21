using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsStorageLocations;

// =========================================================
// FILE: ListOperationsStorageLocationsResponse.cs
//
// PURPOSE:
// The shelves of one warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsStorageLocationsResponse
{
    private ListOperationsStorageLocationsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsWarehouse? Warehouse { get; private init; }

    public IReadOnlyList<OperationsStorageLocation> Locations { get; private init; } = Array.Empty<OperationsStorageLocation>();

    public static ListOperationsStorageLocationsResponse Success(OperationsWarehouse? warehouse, IReadOnlyList<OperationsStorageLocation> locations)
    {
        return new ListOperationsStorageLocationsResponse { Succeeded = true, Warehouse = warehouse, Locations = locations };
    }

    public static ListOperationsStorageLocationsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsStorageLocationsResponse { Error = error };
    }
}
