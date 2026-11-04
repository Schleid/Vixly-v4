using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.ReplyMyOperationsTicket;

// =========================================================
// FILE: ReplyMyOperationsTicketRequest.cs
//
// PURPOSE:
// The customer answers on its ticket; it is open again for staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReplyMyOperationsTicketRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid TicketId { get; init; }

    public string? Message { get; init; }

    // The form's key: the same form sent again changes nothing (ADR-0019).
    public string CommandKey { get; init; } = string.Empty;
}
