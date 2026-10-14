using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTenantNoticeHandler.cs
//
// PURPOSE:
// Emails a tenant's contacts when its access changes (D-T18).
//
// RESPONSIBILITIES:
// - Send a notice on suspension, reactivation, deactivation and
//   ban
// - Reach the Owner and, if different, the Billing contact
//
// IMPORTANT NOTES:
// - The notice is in the tenant's default language
// - The internal reason is never sent; only the category
// - Activation of a new tenant is not noticed here: onboarding
//   emails belong to Operations
// =========================================================
public sealed class PlatformTenantNoticeHandler : IPlatformOutboxHandler
{
    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformTenantMailer _mailer;

    public PlatformTenantNoticeHandler(IPlatformTenantRepository tenants, IPlatformTenantMailer mailer)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.TenantStatusChanged;

    public static bool IsNoticed(PlatformTenantStatus? from, PlatformTenantStatus to)
    {
        return to is PlatformTenantStatus.Suspended or PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned
            || (to == PlatformTenantStatus.Active && from is PlatformTenantStatus.Suspended or PlatformTenantStatus.Deactivated);
    }

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformTenantStatusChanged change = PlatformOutboxWriter.Read<PlatformTenantStatusChanged>(message);

        if (!IsNoticed(change.FromStatus, change.ToStatus))
        {
            return;
        }

        PlatformTenant? tenant = await _tenants.GetAsync(change.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Contacts.Count == 0)
        {
            return;
        }

        PlatformTenantContact owner = tenant.ContactFor(PlatformTenantContactRole.Owner);
        PlatformTenantContact billing = tenant.ContactFor(PlatformTenantContactRole.Billing);

        await _mailer.SendStatusNoticeAsync(tenant, owner, change.ToStatus, change.Category, cancellationToken);

        if (billing.Email != owner.Email)
        {
            await _mailer.SendStatusNoticeAsync(tenant, billing, change.ToStatus, change.Category, cancellationToken);
        }
    }
}
