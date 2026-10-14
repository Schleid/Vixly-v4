using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformSubscriptionMailer.cs
//
// PURPOSE:
// Sends subscription notices by email (D-S13).
//
// RESPONSIBILITIES:
// - Write the price-increase notice in the tenant language and send it
//
// IMPORTANT NOTES:
// - Amounts are before tax and shown with the plan currency
// - Never carries internal notes or reasons
// =========================================================
public sealed class SmtpPlatformSubscriptionMailer : IPlatformSubscriptionMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformSubscriptionMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendPriceIncreaseAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        string planName,
        PlatformSubscriptionTerm term,
        decimal currentAmount,
        decimal newAmount,
        string currencyCode,
        DateTimeOffset effectiveUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);

        CultureInfo culture = CultureInfo.GetCultureInfo(tenant.DefaultLanguage.Value);

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text => (
                VixlyText.Format(text, "email.price_increase.subject", "plan", planName),
                VixlyText.Format(
                    text,
                    "email.price_increase.body",
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.Name,
                        ["company"] = tenant.Company.DisplayName,
                        ["plan"] = planName,
                        ["term"] = text[term == PlatformSubscriptionTerm.Annual ? "ui.subscriptions.term.annual" : "ui.subscriptions.term.monthly"].Value,
                        ["current"] = currentAmount.ToString("N2", culture) + " " + currencyCode,
                        ["new"] = newAmount.ToString("N2", culture) + " " + currencyCode,
                        ["date"] = effectiveUtc.UtcDateTime.ToString("D", culture),
                        ["support"] = _sender.SupportAddress
                    })),
            cancellationToken);
    }
}
