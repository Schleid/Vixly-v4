using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ListOperationsInspectionQueue;

// =========================================================
// FILE: ListOperationsInspectionQueueResponse.cs
//
// PURPOSE:
// The inspection queue of the warehouses a person covers, oldest first (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsInspectionQueueResponse
{
    private ListOperationsInspectionQueueResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsInspectionQueueItem> Items { get; private init; } = Array.Empty<OperationsInspectionQueueItem>();

    public int TotalCount { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsInspectionQueueResponse Success(IReadOnlyList<OperationsInspectionQueueItem> items, int totalCount, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new ListOperationsInspectionQueueResponse { Succeeded = true, Items = items, TotalCount = totalCount, Warehouses = warehouses };
    }

    public static ListOperationsInspectionQueueResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsInspectionQueueResponse { Error = error };
    }
}
