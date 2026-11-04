using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.OpenOperationsReviewTicket;

// =========================================================
// FILE: OpenOperationsReviewTicketResponse.cs
//
// PURPOSE:
// Result of OpenOperationsReviewTicket.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsReviewTicketResponse
{
    private OpenOperationsReviewTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid TicketId { get; private init; }

    public static OpenOperationsReviewTicketResponse Success(Guid ticketId)
    {
        return new OpenOperationsReviewTicketResponse { Succeeded = true, TicketId = ticketId };
    }

    public static OpenOperationsReviewTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsReviewTicketResponse { Error = error };
    }
}
