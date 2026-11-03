using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsOnHandParcels;

// =========================================================
// FILE: ListOperationsOnHandParcelsResponse.cs
//
// PURPOSE:
// Lists the parcels physically on hand in the viewer's warehouses (owner, 2026-10-22: parcel inventory).
//
// RESPONSIBILITIES:
// - The page of parcels, their totals (pieces and weight over all pages), the warehouses and the shelves to filter by
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsOnHandParcelsResponse
{
    private ListOperationsOnHandParcelsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsParcelSummary> Parcels { get; private init; } = Array.Empty<OperationsParcelSummary>();

    public int TotalCount { get; private init; }

    public decimal TotalWeightKg { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    // The shelves of the chosen warehouse (or of the only one), for the shelf filter.
    public IReadOnlyList<OperationsStorageLocation> Locations { get; private init; } = Array.Empty<OperationsStorageLocation>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public DateTimeOffset UtcNow { get; private init; }

    public static ListOperationsOnHandParcelsResponse Success(IReadOnlyList<OperationsParcelSummary> parcels, int totalCount, decimal totalWeightKg,
        IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyList<OperationsStorageLocation> locations, OperationsWeightUnit weightUnit, DateTimeOffset utcNow)
    {
        return new ListOperationsOnHandParcelsResponse
        {
            Succeeded = true,
            Parcels = parcels,
            TotalCount = totalCount,
            TotalWeightKg = totalWeightKg,
            Warehouses = warehouses,
            Locations = locations,
            WeightUnit = weightUnit,
            UtcNow = utcNow
        };
    }

    public static ListOperationsOnHandParcelsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsOnHandParcelsResponse { Error = error };
    }
}
