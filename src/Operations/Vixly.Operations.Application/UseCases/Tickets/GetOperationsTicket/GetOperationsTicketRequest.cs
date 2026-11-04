using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicket;

// =========================================================
// FILE: GetOperationsTicketRequest.cs
//
// PURPOSE:
// One ticket for staff: what the customer knows, the messages, and the undeclared packages that resemble it (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsTicketRequest
{
    public Guid TicketId { get; init; }
}
