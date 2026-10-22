using System.Globalization;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformLeadMailer.cs
//
// PURPOSE:
// Emails a sales staff member about a new contact request (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Render the email in the staff member's language with a console link
//
// IMPORTANT NOTES:
// - The visitor's text is included as plain text; mail is sent as text, never HTML
// =========================================================
public sealed class SmtpPlatformLeadMailer : IPlatformLeadMailer
{
    private readonly PlatformMailSender _sender;

    private readonly PlatformHostingOptions _hosting;

    public SmtpPlatformLeadMailer(PlatformMailSender sender, IOptions<PlatformHostingOptions> hosting)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
    }

    public Task SendLeadReceivedAsync(PlatformPermissionHolderItem recipient, PlatformSalesLead lead, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(lead);

        LanguageCode language = LanguageCode.TryCreate(recipient.Language, out LanguageCode? parsed) ? parsed : LanguageCode.Default;

        return _sender.SendAsync(
            recipient.Email,
            recipient.FirstName + " " + recipient.LastName,
            language,
            text => (
                VixlyText.Format(text, "email.lead.subject", "company", lead.CompanyName),
                VixlyText.Format(
                    text,
                    "email.lead.body",
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.FirstName,
                        ["company"] = lead.CompanyName,
                        ["contact"] = lead.ContactName,
                        ["email"] = lead.Email,
                        ["phone"] = lead.Phone ?? "-",
                        ["country"] = lead.CountryCode ?? "-",
                        ["kind"] = text["ui.leads.kind." + KindCode(lead.Kind)].Value,
                        ["language"] = lead.Language.Value,
                        ["message"] = lead.Message ?? "-",
                        ["link"] = _hosting.ConsoleBaseUrl.TrimEnd('/') + "/leads/" + lead.Id.ToString("D", CultureInfo.InvariantCulture)
                    })),
            cancellationToken);
    }

    public static string KindCode(PlatformSalesLeadKind kind) => kind switch
    {
        PlatformSalesLeadKind.Courier => "courier",
        PlatformSalesLeadKind.Freight => "freight",
        PlatformSalesLeadKind.Warehouse => "warehouse",
        PlatformSalesLeadKind.MultiBranch => "multi_branch",
        _ => "other"
    };
}
