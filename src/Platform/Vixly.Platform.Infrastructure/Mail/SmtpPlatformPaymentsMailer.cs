using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformPaymentsMailer.cs
//
// PURPOSE:
// Sends card-payment emails to a tenant's billing contact (D-P14).
//
// RESPONSIBILITIES:
// - Send the save-card link
// - Tell the tenant a charge failed or needs them, with a pay link
//
// IMPORTANT NOTES:
// - Text comes from PlatformText (L-10): email.save_card.*, email.card_failed.*, email.card_action_required.*
// - In the tenant's language; never provider text
// =========================================================
internal sealed class SmtpPlatformPaymentsMailer : IPlatformPaymentsMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformPaymentsMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendSaveCardLinkAsync(PlatformTenant tenant, PlatformTenantContact recipient, string link, DateTimeOffset expiresUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);

        CultureInfo culture = CultureInfo.GetCultureInfo(tenant.DefaultLanguage.Value);

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text => (
                text["email.save_card.subject"].Value,
                VixlyText.Format(text, "email.save_card.body", new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["name"] = recipient.Name,
                    ["company"] = tenant.Company.DisplayName,
                    ["link"] = link,
                    ["expires"] = expiresUtc.UtcDateTime.ToString("D", culture),
                    ["support"] = _sender.SupportAddress
                })),
            cancellationToken);
    }

    public Task SendCardProblemAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformInvoice invoice, bool actionRequired, string? payLink, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(invoice);

        CultureInfo culture = CultureInfo.GetCultureInfo(invoice.Language.Value);
        string kind = actionRequired ? "card_action_required" : "card_failed";

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            invoice.Language,
            text => (
                VixlyText.Format(text, $"email.{kind}.subject", "invoice", invoice.Number),
                VixlyText.Format(text, payLink is null ? $"email.{kind}.body_no_link" : $"email.{kind}.body", new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["name"] = recipient.Name,
                    ["company"] = tenant.Company.DisplayName,
                    ["invoice"] = invoice.Number,
                    ["amount"] = invoice.Balance.ToString("N2", culture) + " " + invoice.CurrencyCode,
                    ["link"] = payLink ?? string.Empty,
                    ["support"] = _sender.SupportAddress
                })),
            cancellationToken);
    }
}
