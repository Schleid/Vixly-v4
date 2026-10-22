using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsPaymentNoticeHandler.cs
//
// PURPOSE:
// Sends the email of a payment that did not arrive (Slice 5F, C-6).
//
// RESPONSIBILITIES:
// - Read the notice, find the active customer, send the email in its language with a link to their invoices
//
// IMPORTANT NOTES:
// - No email for a customer no longer active, or without a portal address
// =========================================================
public sealed class OperationsPaymentNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    public OperationsPaymentNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public string MessageType => OperationsPaymentNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsPaymentNotice notice = OperationsOutboxWriter.Read<OperationsPaymentNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer)
        {
            return;
        }

        await _accounts.SendAsync(customer, OperationsPaymentNotice.Kind, _accounts.PortalPage(notice.PortalSlug, "/customer/invoices"), cancellationToken);
    }
}
