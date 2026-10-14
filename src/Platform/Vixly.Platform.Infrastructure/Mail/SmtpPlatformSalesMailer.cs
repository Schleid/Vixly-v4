using System.Globalization;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformSalesMailer.cs
//
// PURPOSE:
// Sends approval requests to approvers and discount-ending notices to tenants.
//
// RESPONSIBILITIES:
// - Write each email from resources, in the reader's language
// - Link approvers to the console Approvals page
//
// IMPORTANT NOTES:
// - Text comes from PlatformText (L-10): email.approval_request.* and email.discount_ending.*
// - Amounts before tax
// =========================================================
public sealed class SmtpPlatformSalesMailer : IPlatformSalesMailer
{
    private readonly PlatformMailSender _sender;

    private readonly PlatformHostingOptions _hosting;

    public SmtpPlatformSalesMailer(PlatformMailSender sender, IOptions<PlatformHostingOptions> hosting)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
    }

    public Task SendApprovalRequestAsync(
        PlatformPermissionHolderItem recipient,
        string kindKey,
        string subject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        LanguageCode language = LanguageCode.TryCreate(recipient.Language, out LanguageCode? parsed) ? parsed : LanguageCode.Default;

        return _sender.SendAsync(
            recipient.Email,
            recipient.FirstName + " " + recipient.LastName,
            language,
            text => (
                VixlyText.Format(text, "email.approval_request.subject", "kind", text[kindKey].Value),
                VixlyText.Format(
                    text,
                    "email.approval_request.body",
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.FirstName,
                        ["kind"] = text[kindKey].Value,
                        ["subject"] = subject,
                        ["link"] = _hosting.ConsoleBaseUrl.TrimEnd('/') + "/approvals"
                    })),
            cancellationToken);
    }

    public Task SendDiscountEndingAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        string planName,
        DateTimeOffset endsUtc,
        decimal nextPeriodAmount,
        string currencyCode,
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
                VixlyText.Format(text, "email.discount_ending.subject", "plan", planName),
                VixlyText.Format(
                    text,
                    "email.discount_ending.body",
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.Name,
                        ["company"] = tenant.Company.DisplayName,
                        ["plan"] = planName,
                        ["date"] = endsUtc.UtcDateTime.ToString("D", culture),
                        ["amount"] = nextPeriodAmount.ToString("N2", culture) + " " + currencyCode,
                        ["support"] = _sender.SupportAddress
                    })),
            cancellationToken);
    }
}
