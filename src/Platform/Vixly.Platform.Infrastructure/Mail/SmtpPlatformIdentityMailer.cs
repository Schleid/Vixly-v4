using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformIdentityMailer.cs
//
// PURPOSE:
// Sends staff identity emails: invitation, password reset and
// security notices.
//
// RESPONSIBILITIES:
// - Build each email from resources in the user's language
// - Put the console link with the token into the body
//
// IMPORTANT NOTES:
// - Delivery goes through PlatformMailSender
// - Tokens appear only in the link; never logged (S-11)
// =========================================================
public sealed class SmtpPlatformIdentityMailer : IPlatformIdentityMailer
{
    private readonly PlatformMailSender _sender;

    private readonly PlatformHostingOptions _hosting;

    public SmtpPlatformIdentityMailer(PlatformMailSender sender, IOptions<PlatformHostingOptions> hosting)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
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

    private Task SendAsync(
        PlatformUser user,
        string subjectKey,
        string bodyKey,
        IReadOnlyDictionary<string, object>? values,
        CancellationToken cancellationToken)
    {
        return _sender.SendAsync(
            user.Email.Value,
            $"{user.FirstName} {user.LastName}",
            user.PreferredLanguage,
            text => (VixlyText.Format(text, subjectKey), VixlyText.Format(text, bodyKey, values)),
            cancellationToken);
    }

    private string Link(string path, string token)
    {
        return $"{_hosting.ConsoleBaseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
    }
}
