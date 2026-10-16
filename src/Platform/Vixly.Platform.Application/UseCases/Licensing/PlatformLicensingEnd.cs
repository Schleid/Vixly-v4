using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformLicensingEnd.cs
//
// PURPOSE:
// Revokes all of a tenant's open devices in one commit (D-L9).
//
// RESPONSIBILITIES:
// - Revoke, give seats back, record, email the owner once
//
// IMPORTANT NOTES:
// - Used by the account-closed and subscription-ended handlers
// =========================================================
public static class PlatformLicensingEnd
{
    public static async Task RevokeAllAsync(PlatformLicensingAdministration licensing, Guid tenantId, PlatformDeviceReason reason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(licensing);

        IReadOnlyList<PlatformDevice> open = await licensing.Store.ListOpenDevicesAsync(tenantId, cancellationToken);

        if (open.Count == 0)
        {
            return;
        }

        PlatformTenantLicensing seats = await licensing.SeatsAsync(tenantId, cancellationToken);

        foreach (PlatformDevice device in open)
        {
            licensing.Record(device, device.Revoke(reason, PlatformDeviceActor.System, licensing.UtcNow), seats, int.MaxValue, null, "platform.licensing.device_revoked", null);
        }

        licensing.Notice(tenantId, PlatformLicensingNoticeKind.DevicesRevoked, count: open.Count);

        if (!await licensing.SaveAsync(cancellationToken))
        {
            throw new InvalidOperationException("Devices could not be revoked; the message will be retried.");
        }
    }
}
