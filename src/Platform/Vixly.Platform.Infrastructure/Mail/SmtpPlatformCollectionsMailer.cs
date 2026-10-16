using System.Globalization;
using System.Text;
using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformCollectionsMailer.cs
//
// PURPOSE:
// Writes the collections emails in the tenant's language (D-C11).
//
// RESPONSIBILITIES:
// - Build one email from the step's text, the invoice lines, the totals and the payment instructions
//
// IMPORTANT NOTES:
// - Plain text; dates and amounts in the tenant's culture
// - Never includes an internal note or reason
// =========================================================
internal sealed class SmtpPlatformCollectionsMailer : IPlatformCollectionsMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformCollectionsMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformCollectionsMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(mail);

        CultureInfo culture = CultureInfo.GetCultureInfo(tenant.DefaultLanguage.Value);
        string kind = Key(mail.Kind);

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text =>
            {
                StringBuilder body = new();
                body.AppendLine(VixlyText.Format(text, $"email.collections.{kind}.body", new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["name"] = recipient.Name,
                    ["company"] = tenant.Company.DisplayName,
                    ["blockDate"] = mail.BlockDateUtc?.UtcDateTime.ToString("D", culture) ?? string.Empty,
                    ["through"] = mail.ExtendedThrough?.ToString("D", culture) ?? string.Empty
                }));

                if (mail.Invoices.Count > 0)
                {
                    body.AppendLine();

                    foreach (PlatformCollectionsMailInvoice invoice in mail.Invoices)
                    {
                        body.AppendLine(VixlyText.Format(text, invoice.PayLink is null ? "email.collections.invoice_line_no_link" : "email.collections.invoice_line_online", new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            ["invoice"] = invoice.Number,
                            ["due"] = invoice.DueUtc.UtcDateTime.ToString("d", culture),
                            ["amount"] = invoice.Balance.ToString("N2", culture) + " " + invoice.CurrencyCode,
                            ["link"] = invoice.PayLink ?? string.Empty
                        }));
                    }

                    body.AppendLine();
                    body.AppendLine(VixlyText.Format(text, "email.collections.total", "amounts",
                        string.Join(", ", mail.Totals.Select(total => total.Amount.ToString("N2", culture) + " " + total.CurrencyCode))));
                }

                if (!string.IsNullOrWhiteSpace(mail.PaymentInstructions) && mail.Kind != PlatformCollectionsNoticeKind.AccessRestored)
                {
                    body.AppendLine();
                    body.AppendLine(text["email.collections.instructions"].Value);
                    body.AppendLine(mail.PaymentInstructions);
                }

                body.AppendLine();
                body.Append(VixlyText.Format(text, "email.collections.closing", "support", _sender.SupportAddress));

                string subject = VixlyText.Format(text, $"email.collections.{kind}.subject", new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["company"] = tenant.Company.DisplayName,
                    ["blockDate"] = mail.BlockDateUtc?.UtcDateTime.ToString("d", culture) ?? string.Empty,
                    ["through"] = mail.ExtendedThrough?.ToString("d", culture) ?? string.Empty
                });

                return (subject, body.ToString());
            },
            cancellationToken);
    }

    private static string Key(PlatformCollectionsNoticeKind kind)
    {
        return kind switch
        {
            PlatformCollectionsNoticeKind.Reminder1 => "reminder_1",
            PlatformCollectionsNoticeKind.Reminder2 => "reminder_2",
            PlatformCollectionsNoticeKind.FinalWarning => "final_warning",
            PlatformCollectionsNoticeKind.Blocked => "blocked",
            PlatformCollectionsNoticeKind.ExtensionGranted => "extension_granted",
            PlatformCollectionsNoticeKind.ExtensionEnding => "extension_ending",
            _ => "access_restored"
        };
    }
}
