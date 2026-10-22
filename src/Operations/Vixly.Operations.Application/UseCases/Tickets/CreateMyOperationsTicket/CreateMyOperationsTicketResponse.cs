using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.CreateMyOperationsTicket;

// =========================================================
// FILE: CreateMyOperationsTicketResponse.cs
//
// PURPOSE:
// A customer opens a ticket about a package the carrier delivered that is not in its account (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateMyOperationsTicketResponse
{
    private CreateMyOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid TicketId { get; private init; }

    public static CreateMyOperationsTicketResponse Success(Guid ticketId)
    {
        return new CreateMyOperationsTicketResponse { Succeeded = true, TicketId = ticketId };
    }

    public static CreateMyOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateMyOperationsTicketResponse { Error = error };
    }
}
