using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Locations.ListOperationsLocations;

// =========================================================
// FILE: ListOperationsLocationsResponse.cs
//
// PURPOSE:
// Branches, warehouses and country contacts for the Owner editor (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsLocationsResponse
{
    private ListOperationsLocationsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsBranch> Branches { get; private init; } = Array.Empty<OperationsBranch>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsCountryContact> Contacts { get; private init; } = Array.Empty<OperationsCountryContact>();

    // Slice 4B: the staff who can manage a location.
    public IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsChoice> Managers { get; private init; } = Array.Empty<Vixly.Operations.Application.Shipments.OperationsChoice>();

    public static ListOperationsLocationsResponse Success(IReadOnlyList<OperationsBranch> branches, IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyList<OperationsCountryContact> contacts,
        IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsChoice>? managers = null)
    {
        return new ListOperationsLocationsResponse
        {
            Succeeded = true, Branches = branches, Warehouses = warehouses, Contacts = contacts,
            Managers = managers ?? Array.Empty<Vixly.Operations.Application.Shipments.OperationsChoice>()
        };
    }

    public static ListOperationsLocationsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsLocationsResponse { Error = error };
    }
}
