using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.ReplyMyOperationsTicket;

// =========================================================
// FILE: ReplyMyOperationsTicketResponse.cs
//
// PURPOSE:
// The customer answers on its ticket; it is open again for staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReplyMyOperationsTicketResponse
{
    private ReplyMyOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReplyMyOperationsTicketResponse Success()
    {
        return new ReplyMyOperationsTicketResponse { Succeeded = true };
    }

    public static ReplyMyOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReplyMyOperationsTicketResponse { Error = error };
    }
}
