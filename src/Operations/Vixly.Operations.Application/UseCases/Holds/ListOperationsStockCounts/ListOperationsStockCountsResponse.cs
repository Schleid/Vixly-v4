using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.ListOperationsStockCounts;

// =========================================================
// FILE: ListOperationsStockCountsResponse.cs
//
// PURPOSE:
// The counts of the undeclared parcels area of the warehouses the staff member may see, newest first (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsStockCountsResponse
{
    private ListOperationsStockCountsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsStockCount> Counts { get; private init; } = Array.Empty<OperationsStockCount>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsStockCountsResponse Success(IReadOnlyList<OperationsStockCount> counts, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new ListOperationsStockCountsResponse { Succeeded = true, Counts = counts, Warehouses = warehouses };
    }

    public static ListOperationsStockCountsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsStockCountsResponse { Error = error };
    }
}
