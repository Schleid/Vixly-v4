using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLicensingEndHandler.cs
//
// PURPOSE:
// Revokes every device when the account is deactivated or banned (D-L9).
//
// RESPONSIBILITIES:
// - Revoke open devices, give the seats back, record history, one commit per tenant
// - Email the owner how many devices were revoked
//
// IMPORTANT NOTES:
// - Suspension and blocking for non-payment revoke nothing (D-C17)
// - Replayed messages change nothing: revoked devices are final
// =========================================================
public sealed class PlatformLicensingEndHandler : IPlatformOutboxHandler
{
    private readonly PlatformLicensingAdministration _licensing;

    public PlatformLicensingEndHandler(PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public string MessageType => PlatformMessageTypes.TenantStatusChanged;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformTenantStatusChanged change = PlatformOutboxWriter.Read<PlatformTenantStatusChanged>(message);

        if (change.ToStatus is PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned)
        {
            await PlatformLicensingEnd.RevokeAllAsync(_licensing, change.PlatformTenantId, PlatformDeviceReason.TenantClosed, cancellationToken);
        }
    }
}
