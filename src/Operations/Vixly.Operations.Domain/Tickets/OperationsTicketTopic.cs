namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicketTopic.cs
//
// PURPOSE:
// What a ticket is about; the topic decides the department that sees and handles it (owner, 2026-10-29, ticket desk T-1).
//
// RESPONSIBILITIES:
// - Name the topics
//
// IMPORTANT NOTES:
// - Stored as a number; Package (0) is every ticket from before the desk
// - Who handles each topic: OperationsTicketDepartments (Application)
// =========================================================
public enum OperationsTicketTopic
{
    Package = 0,
    Billing = 1,
    PickupDelivery = 2,
    Claim = 3,
    Other = 4
}
