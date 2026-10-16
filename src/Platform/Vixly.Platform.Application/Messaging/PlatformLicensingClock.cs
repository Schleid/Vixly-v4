using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLicensingClock.cs
//
// PURPOSE:
// Expires devices left pending for 7 days (D-L3).
//
// RESPONSIBILITIES:
// - Expire pending devices in batches, one device per commit
//
// IMPORTANT NOTES:
// - Runs in the subscription clock host until the Platform Worker opens (ADR-0007)
// =========================================================
public sealed class PlatformLicensingClock
{
    private const int BatchSize = 50;

    private readonly PlatformLicensingAdministration _licensing;

    public PlatformLicensingClock(PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        int expired = 0;

        foreach (Guid deviceId in await _licensing.Store.ListExpiredPendingAsync(_licensing.UtcNow - PlatformLicensingLimits.PendingLifetime, BatchSize, cancellationToken))
        {
            PlatformDevice? device = await _licensing.Store.GetDeviceAsync(deviceId, cancellationToken);

            if (device?.ExpireIfDue(_licensing.UtcNow) is not { } change)
            {
                continue;
            }

            _licensing.Record(device, change, await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), 0, null, "platform.licensing.device_expired", null);
            expired += await _licensing.SaveAsync(cancellationToken) ? 1 : 0;
        }

        return expired;
    }
}
