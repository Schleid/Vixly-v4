using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsParcels;

// =========================================================
// FILE: ListOperationsParcelsResponse.cs
//
// PURPOSE:
// Lists the parcels a staff member may see (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsParcelsResponse
{
    private ListOperationsParcelsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsParcelSummary> Parcels { get; private init; } = Array.Empty<OperationsParcelSummary>();

    public int TotalCount { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public static ListOperationsParcelsResponse Success(IReadOnlyList<OperationsParcelSummary> parcels, int totalCount, IReadOnlyList<OperationsWarehouse> warehouses, OperationsWeightUnit weightUnit)
    {
        return new ListOperationsParcelsResponse { Succeeded = true, Parcels = parcels, TotalCount = totalCount, Warehouses = warehouses, WeightUnit = weightUnit };
    }

    public static ListOperationsParcelsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsParcelsResponse { Error = error };
    }
}
