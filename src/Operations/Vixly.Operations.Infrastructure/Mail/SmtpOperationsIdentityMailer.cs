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

    public Task SendDeviceBlockedAsync(OperationsDeviceBlockedMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = mail.ToName,
            ["company"] = mail.CompanyName,
            ["device"] = mail.DeviceName,
            ["person"] = mail.PersonName,
            ["registered"] = mail.RegisteredCountry,
            ["country"] = mail.Country,
            ["time"] = mail.BlockedUtc.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) + " UTC",
            ["link"] = mail.Link
        };

        return _sender.SendAsync(
            mail.ToAddress,
            mail.ToName,
            mail.CompanyName,
            mail.Language,
            text => (VixlyText.Format(text, "email.device_blocked.subject", values), VixlyText.Format(text, "email.device_blocked.body", values)),
            cancellationToken);
    }

    // Owner, 2026-10-29 (ticket desk): a ticket of the manager's department waits too long.
    public Task SendShipmentCapAsync(OperationsShipmentCapMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        string key = mail.Level switch
        {
            >= 150 => mail.GraceMonth ? "email.shipment_cap_grace" : "email.shipment_cap_stopped",
            >= 125 => "email.shipment_cap_last",
            >= 100 => "email.shipment_cap_reached",
            _ => "email.shipment_cap_near"
        };

        return _sender.SendAsync(
            mail.ToAddress,
            mail.ToName,
            mail.CompanyName,
            mail.Language,
            text =>
            {
                Dictionary<string, object> values = new(StringComparer.Ordinal)
                {
                    ["name"] = mail.ToName,
                    ["company"] = mail.CompanyName,
                    ["month"] = mail.Month,
                    ["used"] = mail.Used,
                    ["cap"] = mail.Cap,
                    ["block"] = mail.BlockAt,
                    ["price"] = mail.ExtraPrice,
                    ["link"] = mail.Link
                };
                return (VixlyText.Format(text, key + ".subject", values), VixlyText.Format(text, key + ".body", values));
            },
            cancellationToken);
    }

    public Task SendTicketEscalatedAsync(OperationsTicketEscalatedMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        string key = mail.Step >= 2 ? "email.ticket_unresolved" : "email.ticket_not_taken";

        return _sender.SendAsync(
            mail.ToAddress,
            mail.ToName,
            mail.CompanyName,
            mail.Language,
            text =>
            {
                Dictionary<string, object> values = new(StringComparer.Ordinal)
                {
                    ["name"] = mail.ToName,
                    ["company"] = mail.CompanyName,
                    ["topic"] = text["ui.tickets.topic." + mail.Topic].Value,
                    ["reference"] = mail.Reference,
                    ["time"] = mail.OpenedUtc.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) + " UTC",
                    ["link"] = mail.Link
                };
                return (VixlyText.Format(text, key + ".subject", values), VixlyText.Format(text, key + ".body", values));
            },
            cancellationToken);
    }
}
