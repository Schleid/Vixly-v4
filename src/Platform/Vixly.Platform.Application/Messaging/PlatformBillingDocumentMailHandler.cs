using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformBillingDocumentMailHandler.cs
//
// PURPOSE:
// Emails an issued invoice, credit note or receipt to the tenant's billing contact (D-B13).
//
// RESPONSIBILITIES:
// - Load the stored document and send it
// - Record "emailed" on the invoice history
// - Add a pay link to an open invoice when card payments or Zelle are on (Slice 5)
//
// IMPORTANT NOTES:
// - Sends the stored bytes: the customer gets exactly what is kept
// - The billing contact falls back to the owner
// =========================================================
public sealed class PlatformBillingDocumentMailHandler : IPlatformOutboxHandler
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformBillingMailer _mailer;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformZelleAdministration _zelle;

    public PlatformBillingDocumentMailHandler(
        PlatformBillingAdministration billing,
        IPlatformTenantRepository tenants,
        IPlatformBillingMailer mailer,
        IPlatformUnitOfWork unitOfWork,
        PlatformPaymentsAdministration payments,
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public string MessageType => PlatformMessageTypes.BillingDocumentIssued;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformBillingDocumentIssued issued = PlatformOutboxWriter.Read<PlatformBillingDocumentIssued>(message);
        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(issued.PlatformInvoiceId, cancellationToken);
        PlatformBillingDocument? document = await _billing.Store.GetDocumentAsync(issued.Kind, issued.OwnerId, cancellationToken);
        PlatformTenant? tenant = await _tenants.GetAsync(issued.PlatformTenantId, cancellationToken);

        if (invoice is null || document is null || tenant is null || tenant.Contacts.Count == 0)
        {
            return;
        }

        string? payLink = null;

        // The link is saved before the email goes out, so it always works.
        if (document.Kind == PlatformBillingDocumentKind.Invoice && invoice.Status == PlatformInvoiceStatus.Issued && invoice.Balance > 0m
            && (await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is null || await _zelle.OfferAsync(invoice, cancellationToken) is not null))
        {
            (string token, _) = _payments.IssueLink(PlatformPaymentLinkKind.PayInvoice, tenant.Id, invoice.Id, null);

            // Not saved: retry the whole message rather than mail a link that does not exist.
            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                throw new InvalidOperationException("The payment link could not be saved; the message will be retried.");
            }

            payLink = _payments.Links.PayLink(token);
        }

        await _mailer.SendDocumentAsync(tenant, tenant.ContactFor(PlatformTenantContactRole.Billing), invoice, document, payLink, cancellationToken);
        _billing.Invoices.AddEvents(new[]
        {
            PlatformInvoiceEvent.Record(
                invoice.Id,
                PlatformInvoiceEventCodes.Emailed,
                null,
                _billing.UtcNow,
                arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["number"] = document.Number, ["kind"] = document.Kind.ToString() })
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
