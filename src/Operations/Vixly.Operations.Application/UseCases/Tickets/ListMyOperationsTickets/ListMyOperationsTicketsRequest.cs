using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.ListMyOperationsTickets;

// =========================================================
// FILE: ListMyOperationsTicketsRequest.cs
//
// PURPOSE:
// The customer's tickets, newest first (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListMyOperationsTicketsRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
