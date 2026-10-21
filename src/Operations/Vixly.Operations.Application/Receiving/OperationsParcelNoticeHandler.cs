using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelNoticeHandler.cs
//
// PURPOSE:
// Emails a customer that a package arrived or needs an answer (Slice 3B, design section 7).
//
// RESPONSIBILITIES:
// - Load the customer inside the message's tenant and send the email with the link to the package page
//
// IMPORTANT NOTES:
// - Runs from the outbox, so a mail outage never loses the notice or undoes the receipt
// - A customer who is no longer active gets nothing
// =========================================================
public sealed class OperationsParcelNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    public OperationsParcelNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public string MessageType => OperationsReceivingMessages.ParcelNotice;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsParcelNotice notice = OperationsOutboxWriter.Read<OperationsParcelNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer)
        {
            return;
        }

        await _accounts.SendAsync(customer, notice.Kind, _accounts.PortalPage(notice.PortalSlug, "/customer/packages/" + notice.ParcelId), cancellationToken);
    }
}
