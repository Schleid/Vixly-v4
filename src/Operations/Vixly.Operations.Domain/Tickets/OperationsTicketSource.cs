namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicketSource.cs
//
// PURPOSE:
// Who opened a ticket (owner, 2026-10-29, ticket desk T-2).
//
// RESPONSIBILITIES:
// - Name the sources: the customer in the portal, or a support agent during a call
//
// IMPORTANT NOTES:
// - Stored as a number; only a customer's own tickets are shown to the customer and answered by email
// =========================================================
public enum OperationsTicketSource
{
    Customer = 0,
    Agent = 1,

    // Opened by staff from a customer review (owner, 2026-11-03).
    Review = 2
}
