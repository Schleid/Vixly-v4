using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketNoticeHandler.cs
//
// PURPOSE:
// Sends the customer the email that staff answered its ticket, with a link to it (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Read the notice, find the active customer, send the email in its language
//
// IMPORTANT NOTES:
// - No email for a customer no longer active, or without a portal address
// =========================================================
public sealed class OperationsTicketNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    public OperationsTicketNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public string MessageType => OperationsTicketNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsTicketNotice notice = OperationsOutboxWriter.Read<OperationsTicketNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer)
        {
            return;
        }

        await _accounts.SendAsync(customer, OperationsTicketNotice.Kind, _accounts.PortalPage(notice.PortalSlug, "/customer/tickets/" + notice.TicketId), cancellationToken);
    }
}
