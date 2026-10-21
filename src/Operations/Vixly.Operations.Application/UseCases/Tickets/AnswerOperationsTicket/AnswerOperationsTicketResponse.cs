using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.AnswerOperationsTicket;

// =========================================================
// FILE: AnswerOperationsTicketResponse.cs
//
// PURPOSE:
// Staff answer a ticket, or resolve it with the package they found (now the customer's) or a last word (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AnswerOperationsTicketResponse
{
    private AnswerOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AnswerOperationsTicketResponse Success()
    {
        return new AnswerOperationsTicketResponse { Succeeded = true };
    }

    public static AnswerOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AnswerOperationsTicketResponse { Error = error };
    }
}
