using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformIdentityMailer.cs
//
// PURPOSE:
// Sends the identity emails: invitation, password reset and
// security notices.
//
// RESPONSIBILITIES:
// - Build the link from configuration (PlatformHosting) and
//   the raw token
// - Render the email in the user's preferred language (L-4)
//
// IMPORTANT NOTES:
// - The raw token reaches only the email; it is never logged
// - Called after the change is saved, so a failed send never
//   leaves a link the database does not know
// =========================================================
public interface IPlatformIdentityMailer
{
    Task SendInvitationAsync(PlatformUser user, string token, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(PlatformUser user, string token, CancellationToken cancellationToken);

    Task SendSecurityNoticeAsync(PlatformUser user, string noticeCode, CancellationToken cancellationToken);
}
