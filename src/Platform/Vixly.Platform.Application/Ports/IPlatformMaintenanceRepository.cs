using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformMaintenanceRepository.cs
//
// PURPOSE:
// Stores maintenance windows.
//
// RESPONSIBILITIES:
// - Add, find and list windows
// - Find the window a pass link belongs to
//
// IMPORTANT NOTES:
// - Lists read without tracking
// =========================================================
public interface IPlatformMaintenanceRepository
{
    void Add(PlatformMaintenanceWindow window);

    Task<PlatformMaintenanceWindow?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformMaintenanceWindow>> ListOpenAsync(DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<PagedResult<PlatformMaintenanceWindow>> ListPastAsync(DateTimeOffset utcNow, PagedRequest page, CancellationToken cancellationToken);

    Task<PlatformMaintenanceWindow?> FindByPassHashAsync(string passHash, CancellationToken cancellationToken);
}
