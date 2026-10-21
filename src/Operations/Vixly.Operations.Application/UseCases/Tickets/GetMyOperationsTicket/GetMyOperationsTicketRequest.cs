using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.GetMyOperationsTicket;

// =========================================================
// FILE: GetMyOperationsTicketRequest.cs
//
// PURPOSE:
// One of the customer's tickets with its messages (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsTicketRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid TicketId { get; init; }
}
