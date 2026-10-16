using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformLegalMailer.cs
//
// PURPOSE:
// Writes and sends the Terms notices and reminders in the tenant's language.
//
// RESPONSIBILITIES:
// - Pick the text for the step: new version, notice only, reminder, overdue
//
// IMPORTANT NOTES:
// - Dates in the tenant's culture, UTC
// =========================================================
internal sealed class SmtpPlatformLegalMailer : IPlatformLegalMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformLegalMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformLegalMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(mail);

        CultureInfo culture = CultureInfo.GetCultureInfo(tenant.DefaultLanguage.Value);
        string kind = !mail.AcceptanceRequired ? "notice"
            : mail.Step == PlatformTermsReminderStep.Published ? "published"
            : mail.Overdue || (mail.Step >= PlatformTermsReminderStep.After1Week && mail.Step != PlatformTermsReminderStep.Manual) ? "overdue"
            : "reminder";
        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = recipient.Name,
            ["company"] = tenant.Company.DisplayName,
            ["version"] = mail.Version,
            ["deadline"] = mail.DeadlineUtc?.UtcDateTime.ToString("d", culture) ?? string.Empty,
            ["summary"] = mail.Summary ?? string.Empty,
            ["link"] = mail.Link,
            ["support"] = _sender.SupportAddress
        };

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text => (VixlyText.Format(text, $"email.legal.{kind}.subject", values), VixlyText.Format(text, $"email.legal.{kind}.body", values)),
            cancellationToken);
    }
}
