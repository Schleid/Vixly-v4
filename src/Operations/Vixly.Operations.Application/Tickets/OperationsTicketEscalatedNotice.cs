namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketEscalatedNotice.cs
//
// PURPOSE:
// The outbox message of an escalation: tell the managers of a ticket's department (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Carry the ticket, the step and the portal address for the link
//
// IMPORTANT NOTES:
// - Written once per step by OperationsTicketEscalations; sent by OperationsTicketEscalatedHandler
// =========================================================
public sealed record OperationsTicketEscalatedNotice(Guid TicketId, int Step, string PortalSlug)
{
    public const string MessageType = "ops.tickets.escalated.v1";
}
