namespace Vixly.Operations.Application.UseCases.Tickets.MoveOperationsTicket;

// =========================================================
// FILE: MoveOperationsTicketRequest.cs
//
// PURPOSE:
// Moves a ticket to another department by changing its topic (owner, 2026-10-29, ticket desk section 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class MoveOperationsTicketRequest
{
    public Guid TicketId { get; init; }

    public Vixly.Operations.Domain.Tickets.OperationsTicketTopic Topic { get; init; }
}
