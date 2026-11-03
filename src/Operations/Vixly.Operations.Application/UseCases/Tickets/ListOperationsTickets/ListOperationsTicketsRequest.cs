using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.ListOperationsTickets;

// =========================================================
// FILE: ListOperationsTicketsRequest.cs
//
// PURPOSE:
// The tickets of the warehouses the staff member may see, open ones first (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsTicketsRequest
{
    public Guid? WarehouseId { get; init; }

    // Ticket desk (owner, 2026-10-29): new, taken, open, answered, resolved, escalated, or all (null).
    public string? State { get; init; } = "open";

    public OperationsTicketTopic? Topic { get; init; }

    // Only the tickets the person took.
    public bool Mine { get; init; }

    public int Page { get; init; } = 1;
}
