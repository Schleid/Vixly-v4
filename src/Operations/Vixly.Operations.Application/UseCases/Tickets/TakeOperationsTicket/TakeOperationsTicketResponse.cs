using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.TakeOperationsTicket;

// =========================================================
// FILE: TakeOperationsTicketResponse.cs
//
// PURPOSE:
// Someone of the ticket's department takes it (I'm on it), gives it to a colleague of the department, or releases it (owner, 2026-10-29, ticket desk section 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TakeOperationsTicketResponse
{
    private TakeOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static TakeOperationsTicketResponse Success()
    {
        return new TakeOperationsTicketResponse { Succeeded = true };
    }

    public static TakeOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TakeOperationsTicketResponse { Error = error };
    }
}
