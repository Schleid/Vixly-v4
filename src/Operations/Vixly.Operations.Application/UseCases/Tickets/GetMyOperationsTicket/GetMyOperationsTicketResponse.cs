using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.GetMyOperationsTicket;

// =========================================================
// FILE: GetMyOperationsTicketResponse.cs
//
// PURPOSE:
// One of the customer's tickets with its messages (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsTicketResponse
{
    private GetMyOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTicket? Ticket { get; private init; }

    public static GetMyOperationsTicketResponse Success(OperationsTicket? ticket)
    {
        return new GetMyOperationsTicketResponse { Succeeded = true, Ticket = ticket };
    }

    public static GetMyOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsTicketResponse { Error = error };
    }
}
