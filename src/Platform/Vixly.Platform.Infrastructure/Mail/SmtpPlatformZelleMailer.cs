using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformZelleMailer.cs
//
// PURPOSE:
// Writes and sends the Zelle emails in the tenant's language.
//
// RESPONSIBILITIES:
// - Fill the email.zelle.* texts with the invoice, amount, date and reason
//
// IMPORTANT NOTES:
// - Amounts and dates in the tenant's culture
// =========================================================
internal sealed class SmtpPlatformZelleMailer : IPlatformZelleMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformZelleMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformZelleMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(mail);

        CultureInfo culture = CultureInfo.GetCultureInfo(tenant.DefaultLanguage.Value);
        string kind = mail.Kind == PlatformZelleNoticeKind.Rejected ? "rejected" : "received";
        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = recipient.Name,
            ["company"] = tenant.Company.DisplayName,
            ["invoice"] = mail.InvoiceNumber,
            ["amount"] = mail.Amount.ToString("N2", culture) + " " + mail.CurrencyCode,
            ["date"] = mail.SentOn.ToString("d", culture),
            ["reason"] = mail.Reason ?? string.Empty,
            ["support"] = _sender.SupportAddress
        };

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text => (VixlyText.Format(text, $"email.zelle.{kind}.subject", values), VixlyText.Format(text, $"email.zelle.{kind}.body", values)),
            cancellationToken);
    }
}
