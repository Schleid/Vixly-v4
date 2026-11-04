using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.ListMyOperationsTickets;

// =========================================================
// FILE: ListMyOperationsTicketsResponse.cs
//
// PURPOSE:
// The customer's tickets, newest first (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListMyOperationsTicketsResponse
{
    private ListMyOperationsTicketsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsTicket> Tickets { get; private init; } = Array.Empty<OperationsTicket>();

    // The warehouses the customer may name on a new ticket.
    public IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsChoice> Origins { get; private init; } = Array.Empty<Vixly.Operations.Application.Shipments.OperationsChoice>();

    public static ListMyOperationsTicketsResponse Success(IReadOnlyList<OperationsTicket> tickets, IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsChoice> origins)
    {
        return new ListMyOperationsTicketsResponse { Succeeded = true, Tickets = tickets, Origins = origins };
    }

    public static ListMyOperationsTicketsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListMyOperationsTicketsResponse { Error = error };
    }
}
