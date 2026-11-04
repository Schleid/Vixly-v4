using System.Globalization;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformSalesFeedMailer.cs
//
// PURPOSE:
// Emails staff when the sales feed to TechOS needs a person.
//
// RESPONSIBILITIES:
// - Say why (events parked, sending stopped, no answer for 24 hours), how many events and TechOS's answer code
// - Link to the Control sales feed page
//
// IMPORTANT NOTES:
// - Staff email in the recipient's language; never an event body or a secret
// =========================================================
public sealed class SmtpPlatformSalesFeedMailer : IPlatformSalesFeedMailer
{
    private readonly PlatformMailSender _sender;

    private readonly PlatformHostingOptions _hosting;

    public SmtpPlatformSalesFeedMailer(PlatformMailSender sender, IOptions<PlatformHostingOptions> hosting)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
    }

    public Task SendAlertAsync(PlatformPermissionHolderItem recipient, PlatformSalesFeedAlert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(alert);

        LanguageCode language = LanguageCode.TryCreate(recipient.Language, out LanguageCode? parsed) ? parsed : LanguageCode.Default;
        string kind = alert.Kind switch
        {
            PlatformSalesFeedAlertKind.Parked => "parked",
            PlatformSalesFeedAlertKind.Stopped => "stopped",
            _ => "no_answer"
        };

        return _sender.SendAsync(
            recipient.Email,
            recipient.FirstName + " " + recipient.LastName,
            language,
            text => (
                text["email.sales_feed.subject." + kind].Value,
                VixlyText.Format(
                    text,
                    "email.sales_feed.body." + kind,
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.FirstName,
                        ["count"] = alert.Count.ToString(CultureInfo.InvariantCulture),
                        ["code"] = alert.Code,
                        ["link"] = _hosting.ConsoleBaseUrl.Trim().TrimEnd('/') + "/sales-feed"
                    })),
            cancellationToken);
    }
}
