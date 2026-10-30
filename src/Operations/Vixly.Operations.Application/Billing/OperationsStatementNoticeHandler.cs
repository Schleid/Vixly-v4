using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsStatementNoticeHandler.cs
//
// PURPOSE:
// Emails a customer that their monthly statement is ready (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Read the notice, find the active customer, send the email in its language with a link to their account
//
// IMPORTANT NOTES:
// - No email for a customer no longer active
// =========================================================
public sealed class OperationsStatementNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    public OperationsStatementNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public string MessageType => OperationsStatementNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsStatementNotice notice = OperationsOutboxWriter.Read<OperationsStatementNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer)
        {
            return;
        }

        await _accounts.SendAsync(customer, OperationsStatementNotice.Kind, _accounts.PortalPage(notice.PortalSlug, "/customer/account"), cancellationToken);
    }
}
