using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Licensing;

// =========================================================
// FILE: PlatformDeviceLicenceCounter.cs
//
// PURPOSE:
// Counts the devices holding a seat, for Slice 3's licence decrease and downgrade checks (D-L2, D-L9).
//
// RESPONSIBILITIES:
// - Answer how many seats are in use
//
// IMPORTANT NOTES:
// - Replaces the Slice 3 placeholder that always answered 0
// =========================================================
internal sealed class PlatformDeviceLicenceCounter : IPlatformLicenceCounter
{
    private readonly IPlatformLicensingRepository _licensing;

    public PlatformDeviceLicenceCounter(IPlatformLicensingRepository licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public Task<int> CountActiveAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _licensing.CountHoldingSeatsAsync(platformTenantId, cancellationToken);
    }
}
