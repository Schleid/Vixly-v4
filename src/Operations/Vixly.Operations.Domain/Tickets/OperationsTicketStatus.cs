namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicketStatus.cs
//
// PURPOSE:
// Where a customer's ticket stands (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Open (staff to answer), answered (waiting for the customer), resolved
//
// IMPORTANT NOTES:
// - Stored as a number
// =========================================================
public enum OperationsTicketStatus
{
    Open = 1,
    Answered = 2,
    Resolved = 3
}
