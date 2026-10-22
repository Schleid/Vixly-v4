using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.ListOperationsTickets;

// =========================================================
// FILE: ListOperationsTicketsResponse.cs
//
// PURPOSE:
// The tickets of the warehouses the staff member may see, open ones first (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsTicketsResponse
{
    private ListOperationsTicketsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsTicket> Tickets { get; private init; } = Array.Empty<OperationsTicket>();

    public int TotalCount { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsTicketsResponse Success(IReadOnlyList<OperationsTicket> tickets, int totalCount, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new ListOperationsTicketsResponse { Succeeded = true, Tickets = tickets, TotalCount = totalCount, Warehouses = warehouses };
    }

    public static ListOperationsTicketsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsTicketsResponse { Error = error };
    }
}
