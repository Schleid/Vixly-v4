using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.MoveOperationsTicket;

// =========================================================
// FILE: MoveOperationsTicketResponse.cs
//
// PURPOSE:
// Moves a ticket to another department by changing its topic (owner, 2026-10-29, ticket desk section 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class MoveOperationsTicketResponse
{
    private MoveOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static MoveOperationsTicketResponse Success()
    {
        return new MoveOperationsTicketResponse { Succeeded = true };
    }

    public static MoveOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new MoveOperationsTicketResponse { Error = error };
    }
}
