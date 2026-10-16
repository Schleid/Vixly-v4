using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformBillingMailer.cs
//
// PURPOSE:
// Emails invoices, credit notes and receipts with their stored PDF (D-B13).
//
// RESPONSIBILITIES:
// - Compose the email in the document's language and attach the PDF
//
// IMPORTANT NOTES:
// - Text comes from PlatformText (L-10): email.invoice.*, email.credit_note.*, email.receipt.*
// =========================================================
internal sealed class SmtpPlatformBillingMailer : IPlatformBillingMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformBillingMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendDocumentAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        PlatformInvoice invoice,
        PlatformBillingDocument document,
        string? payLink,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(document);

        CultureInfo culture = CultureInfo.GetCultureInfo(document.Language.Value);
        string kind = document.Kind switch
        {
            PlatformBillingDocumentKind.CreditNote => "credit_note",
            PlatformBillingDocumentKind.Receipt => "receipt",
            _ => "invoice"
        };

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            document.Language,
            text => (
                VixlyText.Format(text, $"email.{kind}.subject", "number", document.Number),
                VixlyText.Format(
                    text,
                    $"email.{kind}.body",
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.Name,
                        ["company"] = tenant.Company.DisplayName,
                        ["number"] = document.Number,
                        ["invoice"] = invoice.Number,
                        ["amount"] = invoice.Total.ToString("N2", culture) + " " + invoice.CurrencyCode,
                        ["balance"] = invoice.Balance.ToString("N2", culture) + " " + invoice.CurrencyCode,
                        ["due"] = invoice.DueUtc.UtcDateTime.ToString("D", culture),
                        ["support"] = _sender.SupportAddress
                    })
                    + (payLink is null ? string.Empty : "\n\n" + VixlyText.Format(text, "email.invoice.pay_online", "link", payLink))),
            (document.FileName, document.Content),
            cancellationToken);
    }
}
