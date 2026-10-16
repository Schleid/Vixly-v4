using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLicensingNoticeHandler.cs
//
// PURPOSE:
// Sends the licensing emails to the tenant owner; creates first-device and recovery links as it sends (D-L7, D-L8, D-L16).
//
// RESPONSIBILITIES:
// - Create a link (newer voids older) and save it before mailing it
// - Send to the owner contact only
//
// IMPORTANT NOTES:
// - No token is ever stored in the outbox or shown on a screen
// - A link is created per send: a retried message never mails a dead link
// =========================================================
public sealed class PlatformLicensingNoticeHandler : IPlatformOutboxHandler
{
    private readonly PlatformLicensingAdministration _licensing;

    private readonly IPlatformLicensingMailer _mailer;

    public PlatformLicensingNoticeHandler(PlatformLicensingAdministration licensing, IPlatformLicensingMailer mailer)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.LicensingNotice;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformLicensingNotice notice = PlatformOutboxWriter.Read<PlatformLicensingNotice>(message);
        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(notice.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Contacts.Count == 0)
        {
            return;
        }

        DateTimeOffset utcNow = _licensing.UtcNow;
        string? link = null;
        DateTimeOffset? expires = null;

        if (notice.Kind is PlatformLicensingNoticeKind.FirstDeviceLink or PlatformLicensingNoticeKind.RecoveryLink)
        {
            PlatformDeviceLinkKind kind = notice.Kind == PlatformLicensingNoticeKind.FirstDeviceLink ? PlatformDeviceLinkKind.FirstDevice : PlatformDeviceLinkKind.OwnerRecovery;

            foreach (PlatformDeviceLink older in await _licensing.Store.ListOpenLinksAsync(tenant.Id, kind, cancellationToken))
            {
                older.Void(utcNow);
            }

            PlatformSecret token = _licensing.NewSecret();
            PlatformDeviceLink created = PlatformDeviceLink.Create(kind, tenant.Id, notice.RegistrationId, token.Hash, utcNow);
            _licensing.Store.AddLink(created);

            // Saved before sending, so the link in the email always works; not saved: the message is retried.
            if (!await _licensing.UnitOfWork.SaveChangesAsync(cancellationToken))
            {
                throw new InvalidOperationException("The device link could not be saved; the message will be retried.");
            }

            link = _licensing.Links.DeviceLink(tenant.CurrentSlug, kind, token.Value);
            expires = created.ExpiresUtc;
        }

        PlatformDevice? device = notice.DeviceId is Guid deviceId ? await _licensing.Store.GetDeviceAsync(deviceId, cancellationToken) : null;

        await _mailer.SendAsync(
            tenant,
            tenant.ContactFor(PlatformTenantContactRole.Owner),
            new PlatformLicensingMail(notice.Kind, link, expires, device?.Number, device?.Name, notice.Approved, notice.Count),
            cancellationToken);
    }
}
