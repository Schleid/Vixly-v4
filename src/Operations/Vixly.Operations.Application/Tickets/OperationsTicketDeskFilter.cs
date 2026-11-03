using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketDeskFilter.cs
//
// PURPOSE:
// What the ticket desk list filters on (owner, 2026-10-29, ticket desk section 4).
//
// RESPONSIBILITIES:
// - Carry the state, topic, warehouse and "mine" filters
//
// IMPORTANT NOTES:
// - State: new (open, nobody took it), taken, answered, resolved, escalated, or all (null)
// =========================================================
public sealed record OperationsTicketDeskFilter(string? State, OperationsTicketTopic? Topic, Guid? WarehouseId, Guid? TakenBy);
