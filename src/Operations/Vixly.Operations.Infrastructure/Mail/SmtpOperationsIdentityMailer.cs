using Vixly.Operations.Application.Ports;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: SmtpOperationsIdentityMailer.cs
//
// PURPOSE:
// Staff account emails (Slice 1).
//
// RESPONSIBILITIES:
// - Send the owner invitation
//
// IMPORTANT NOTES:
// - Texts email.owner_invitation.* in OperationsText; tenant branded, never "Vixly"
// =========================================================
public sealed class SmtpOperationsIdentityMailer : IOperationsIdentityMailer
{
    private readonly OperationsMailSender _sender;

    public SmtpOperationsIdentityMailer(OperationsMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendOwnerInvitationAsync(OperationsOwnerInvitationMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = mail.ToName,
            ["company"] = mail.CompanyName,
            ["hours"] = mail.Hours,
            ["link"] = mail.Link
        };

        return _sender.SendAsync(
            mail.ToAddress,
            mail.ToName,
            mail.CompanyName,
            mail.Language,
            text => (VixlyText.Format(text, "email.owner_invitation.subject", values), VixlyText.Format(text, "email.owner_invitation.body", values)),
            cancellationToken);
    }

    public Task SendPasswordResetAsync(OperationsPasswordResetMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = mail.ToName,
            ["company"] = mail.CompanyName,
            ["minutes"] = mail.Minutes,
            ["link"] = mail.Link
        };

        return _sender.SendAsync(
            mail.ToAddress,
            mail.ToName,
            mail.CompanyName,
            mail.Language,
            text => (VixlyText.Format(text, "email.password_reset.subject", values), VixlyText.Format(text, "email.password_reset.body", values)),
            cancellationToken);
    }
}
