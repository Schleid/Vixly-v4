using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformCardProblemMailHandler.cs
//
// PURPOSE:
// Emails the billing contact when a card charge failed or needs them, with a fresh pay link (D-P14).
//
// RESPONSIBILITIES:
// - Issue a pay link while the invoice is still open and cards are accepted
// - Send the email in the tenant's language
//
// IMPORTANT NOTES:
// - No provider text in the email; only what happened and the link
// =========================================================
public sealed class PlatformCardProblemMailHandler : IPlatformOutboxHandler
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformPaymentsMailer _mailer;

    public PlatformCardProblemMailHandler(PlatformPaymentsAdministration payments, IPlatformPaymentsMailer mailer)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.CardPaymentFailed;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformCardPaymentFailed failed = PlatformOutboxWriter.Read<PlatformCardPaymentFailed>(message);
        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(failed.PlatformInvoiceId, cancellationToken);
        PlatformTenant? tenant = await _payments.Tenants.GetAsync(failed.PlatformTenantId, cancellationToken);

        if (invoice is null || tenant is null || tenant.Contacts.Count == 0 || invoice.Status != PlatformInvoiceStatus.Issued)
        {
            return;
        }

        string? payLink = null;

        if (invoice.Balance > 0m && await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is null)
        {
            (string token, _) = _payments.IssueLink(PlatformPaymentLinkKind.PayInvoice, tenant.Id, invoice.Id, null);

            // Not saved: retry the whole message rather than mail a link that does not exist.
            if (!await _payments.UnitOfWork.SaveChangesAsync(cancellationToken))
            {
                throw new InvalidOperationException("The payment link could not be saved; the message will be retried.");
            }

            payLink = _payments.Links.PayLink(token);
        }

        await _mailer.SendCardProblemAsync(tenant, tenant.ContactFor(PlatformTenantContactRole.Billing), invoice, failed.ActionRequired, payLink, cancellationToken);
    }
}
