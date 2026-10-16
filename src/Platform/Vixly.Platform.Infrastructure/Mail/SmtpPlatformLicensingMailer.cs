using System.Globalization;
using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformLicensingMailer.cs
//
// PURPOSE:
// Writes the licensing emails to the tenant owner in the tenant's language (D-L16).
//
// RESPONSIBILITIES:
// - Pick the text for the kind and fill in the link, device and dates
//
// IMPORTANT NOTES:
// - Plain text; the link carries its token after "#"
// =========================================================
internal sealed class SmtpPlatformLicensingMailer : IPlatformLicensingMailer
{
    private readonly PlatformMailSender _sender;

    public SmtpPlatformLicensingMailer(PlatformMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformLicensingMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(mail);

        CultureInfo culture = CultureInfo.GetCultureInfo(tenant.DefaultLanguage.Value);
        string kind = Key(mail.Kind, mail.Approved);
        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = recipient.Name,
            ["company"] = tenant.Company.DisplayName,
            ["link"] = mail.Link ?? string.Empty,
            ["expires"] = mail.LinkExpiresUtc?.UtcDateTime.ToString("g", culture) ?? string.Empty,
            ["device"] = mail.DeviceName ?? string.Empty,
            ["number"] = mail.DeviceNumber ?? string.Empty,
            ["count"] = mail.Count,
            ["support"] = _sender.SupportAddress
        };

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text => (VixlyText.Format(text, $"email.licensing.{kind}.subject", values), VixlyText.Format(text, $"email.licensing.{kind}.body", values)),
            cancellationToken);
    }

    private static string Key(PlatformLicensingNoticeKind kind, bool? approved)
    {
        return kind switch
        {
            PlatformLicensingNoticeKind.FirstDeviceLink => "first_device",
            PlatformLicensingNoticeKind.RecoveryLink => "recovery",
            PlatformLicensingNoticeKind.DeviceAdded => "device_added",
            PlatformLicensingNoticeKind.DeviceReplaced => "device_replaced",
            PlatformLicensingNoticeKind.ReleaseDecided => approved == true ? "release_approved" : "release_refused",
            PlatformLicensingNoticeKind.DevicesRevoked => "devices_revoked",
            _ => "credential_reused"
        };
    }
}
