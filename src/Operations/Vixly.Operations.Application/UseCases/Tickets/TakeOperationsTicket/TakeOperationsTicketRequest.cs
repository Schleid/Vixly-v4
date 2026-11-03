namespace Vixly.Operations.Application.UseCases.Tickets.TakeOperationsTicket;

// =========================================================
// FILE: TakeOperationsTicketRequest.cs
//
// PURPOSE:
// Someone of the ticket's department takes it (I'm on it), gives it to a colleague of the department, or releases it (owner, 2026-10-29, ticket desk section 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TakeOperationsTicketRequest
{
    public Guid TicketId { get; init; }

    public Guid? GiveTo { get; init; }

    public bool Release { get; init; }
}
