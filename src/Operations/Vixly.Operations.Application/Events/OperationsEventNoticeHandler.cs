using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Events;

// =========================================================
// FILE: OperationsEventNoticeHandler.cs
//
// PURPOSE:
// Sends the customer the email of a delay or an exception (Slice 6C, E-5).
//
// RESPONSIBILITIES:
// - Word the cause in the customer's language and link to their shipments
//
// IMPORTANT NOTES:
// - A customer no longer active gets nothing
// =========================================================
public sealed class OperationsEventNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    public OperationsEventNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public string MessageType => OperationsEventNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsEventNotice notice = OperationsOutboxWriter.Read<OperationsEventNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer)
        {
            return;
        }

        string language = customer.PreferredLanguage.Value;
        string? label = language switch { "fr" => notice.LabelFr ?? notice.LabelEn, "es" => notice.LabelEs ?? notice.LabelEn, _ => notice.LabelEn };
        Dictionary<string, string> extra = new(StringComparer.Ordinal)
        {
            ["shipment"] = notice.ShipmentNumber,
            ["message"] = notice.Message ?? string.Empty
        };

        // Words the mailer looks up in the customer's language: keys only, never what a person typed (review).
        Dictionary<string, string> worded = new(StringComparer.Ordinal)
        {
            ["kind"] = "email.customer.shipment_update." + (notice.Exception ? "exception" : "delay"),
            ["item"] = "email.customer.shipment_update." + (notice.Package ? "item_package" : "item_shipment")
        };

        if (label is not null)
        {
            extra["cause"] = label;
        }
        else
        {
            worded["cause"] = "ui.event_reason." + notice.Cause;
        }

        await _accounts.SendAsync(customer, OperationsEventNotice.Kind, _accounts.PortalPage(notice.PortalSlug, notice.Package ? "/customer/packages" : "/customer/shipments"), extra, worded,
            cancellationToken);
    }
}
