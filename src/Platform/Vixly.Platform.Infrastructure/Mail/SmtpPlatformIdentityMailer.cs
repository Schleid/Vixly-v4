using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Platform.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformIdentityMailer.cs
//
// PURPOSE:
// Sends the identity emails over SMTP, in the recipient's
// language (L-4).
//
// RESPONSIBILITIES:
// - Build links from PlatformHosting:ConsoleBaseUrl
// - Render subject and body from PlatformText in the user's
//   preferred language
// - Send through SMTP, or write .eml files to the pickup
//   directory in development
//
// IMPORTANT NOTES:
// - Never logs the body or the link: they hold one-time
//   tokens
// - Plain text only for now; the Frontend/UI team designs the
//   HTML templates later, from the same resource keys
// =========================================================
public sealed class SmtpPlatformIdentityMailer : IPlatformIdentityMailer
{
    private readonly IStringLocalizer<PlatformText> _text;

    private readonly PlatformMailOptions _mail;

    private readonly PlatformHostingOptions _hosting;

    public SmtpPlatformIdentityMailer(
        IStringLocalizer<PlatformText> text,
        IOptions<PlatformMailOptions> mail,
        IOptions<PlatformHostingOptions> hosting)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _mail = mail?.Value ?? throw new ArgumentNullException(nameof(mail));
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
    }

    public Task SendInvitationAsync(PlatformUser user, string token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return SendAsync(
            user,
            "email.invitation.subject",
            "email.invitation.body",
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["name"] = user.FirstName,
                ["hours"] = (int)PlatformIdentityLimits.InvitationLifetime.TotalHours,
                ["link"] = Link("invitation", token)
            },
            cancellationToken);
    }

    public Task SendPasswordResetAsync(PlatformUser user, string token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return SendAsync(
            user,
            "email.reset.subject",
            "email.reset.body",
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["name"] = user.FirstName,
                ["minutes"] = (int)PlatformIdentityLimits.PasswordResetLifetime.TotalMinutes,
                ["link"] = Link("reset-password", token)
            },
            cancellationToken);
    }

    public Task SendSecurityNoticeAsync(PlatformUser user, string noticeCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return SendAsync(user, "email.notice.subject", noticeCode, null, cancellationToken);
    }

    private async Task SendAsync(
        PlatformUser user,
        string subjectKey,
        string bodyKey,
        IReadOnlyDictionary<string, object>? values,
        CancellationToken cancellationToken)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        string subject;
        string body;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(user.PreferredLanguage.Value);
            subject = VixlyText.Format(_text, subjectKey);
            body = VixlyText.Format(_text, bodyKey, values);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        using MailMessage message = new()
        {
            From = new MailAddress(_mail.FromAddress, _mail.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(user.Email.Value, $"{user.FirstName} {user.LastName}"));

        using SmtpClient client = CreateClient();
        await client.SendMailAsync(message, cancellationToken);
    }

    private SmtpClient CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_mail.PickupDirectory))
        {
            Directory.CreateDirectory(_mail.PickupDirectory);

            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = Path.GetFullPath(_mail.PickupDirectory)
            };
        }

        SmtpClient client = new(_mail.SmtpHost, _mail.SmtpPort) { EnableSsl = _mail.UseSsl };

        if (!string.IsNullOrEmpty(_mail.UserName))
        {
            client.Credentials = new NetworkCredential(_mail.UserName, _mail.Password);
        }

        return client;
    }

    private string Link(string path, string token)
    {
        return $"{_hosting.ConsoleBaseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
    }
}
