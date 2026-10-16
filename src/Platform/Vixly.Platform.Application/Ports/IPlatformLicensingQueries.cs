using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLicensingQueries.cs
//
// PURPOSE:
// Read models for the licensing screens (D-L15).
//
// RESPONSIBILITIES:
// - Page devices with filters; read one device and its history
// - Count a tenant's devices by state
//
// IMPORTANT NOTES:
// - Every list is paged (P-1)
// =========================================================
public interface IPlatformLicensingQueries
{
    Task<PagedResult<PlatformDeviceItem>> ListDevicesAsync(PlatformDeviceFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<PlatformDeviceItem?> GetDeviceAsync(Guid deviceId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformDeviceEventItem>> ListEventsAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<PlatformLicensingCounts> CountAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
