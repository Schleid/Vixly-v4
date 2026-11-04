using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsIncoming;

// =========================================================
// FILE: ListOperationsIncomingResponse.cs
//
// PURPOSE:
// The incoming packages list: every pre-alert of the warehouses the staff member may see, filtered (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsIncomingResponse
{
    private ListOperationsIncomingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsIncomingRow> Rows { get; private init; } = Array.Empty<OperationsIncomingRow>();

    public int TotalCount { get; private init; }

    public OperationsIncomingCounts Counts { get; private init; } = new(0, 0, 0);

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public DateOnly Today { get; private init; }

    public static ListOperationsIncomingResponse Success(IReadOnlyList<OperationsIncomingRow> rows, int totalCount, OperationsIncomingCounts counts, IReadOnlyList<OperationsWarehouse> warehouses, DateOnly today)
    {
        return new ListOperationsIncomingResponse { Succeeded = true, Rows = rows, TotalCount = totalCount, Counts = counts, Warehouses = warehouses, Today = today };
    }

    public static ListOperationsIncomingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsIncomingResponse { Error = error };
    }
}
