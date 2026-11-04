using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketNotice.cs
//
// PURPOSE:
// The outbox message that tells a customer staff answered or resolved its ticket (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the customer, the ticket and the portal to link to
//
// IMPORTANT NOTES:
// - Sent by OperationsTicketNoticeHandler after the transaction commits
// =========================================================
public sealed record OperationsTicketNotice(Guid CustomerId, Guid TicketId, string PortalSlug)
{
    public const string MessageType = "ops.tickets.notice.v1";

    public static OperationsCustomerMailKind Kind => OperationsCustomerMailKind.TicketAnswered;
}
