using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.AnswerOperationsTicket;

// =========================================================
// FILE: AnswerOperationsTicketRequest.cs
//
// PURPOSE:
// Staff answer a ticket, or resolve it with the package they found (now the customer's) or a last word (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AnswerOperationsTicketRequest
{
    public Guid TicketId { get; init; }

    public string? Message { get; init; }

    public bool Resolve { get; init; }

    // The package found, already given to the ticket's customer.
    public Guid? ParcelId { get; init; }

    public string PortalSlug { get; init; } = string.Empty;

    // The form's key: the same form sent again changes nothing (ADR-0019).
    public string CommandKey { get; init; } = string.Empty;
}
