using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsReceipts;

// =========================================================
// FILE: ListOperationsReceiptsResponse.cs
//
// PURPOSE:
// The carrier deliveries of the warehouses a person receives at: open ones, then receipts (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsReceiptsResponse
{
    private ListOperationsReceiptsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsDeliverySummary> Open { get; private init; } = Array.Empty<OperationsDeliverySummary>();

    public IReadOnlyList<OperationsWarehouseReceipt> Receipts { get; private init; } = Array.Empty<OperationsWarehouseReceipt>();

    public int TotalCount { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsReceiptsResponse Success(IReadOnlyList<OperationsDeliverySummary> open, IReadOnlyList<OperationsWarehouseReceipt> receipts, int totalCount, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new ListOperationsReceiptsResponse { Succeeded = true, Open = open, Receipts = receipts, TotalCount = totalCount, Warehouses = warehouses };
    }

    public static ListOperationsReceiptsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsReceiptsResponse { Error = error };
    }
}
