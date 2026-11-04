using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeMaintenanceStore.cs
//
// PURPOSE:
// In-memory maintenance windows for the use case tests.
//
// RESPONSIBILITIES:
// - Answer the repository from a list
//
// IMPORTANT NOTES:
// - Test double only
// =========================================================
internal sealed class FakeMaintenanceStore : IPlatformMaintenanceRepository
{
    public List<PlatformMaintenanceWindow> Windows { get; } = new();

    public void Add(PlatformMaintenanceWindow window)
    {
        Windows.Add(window);
    }

    public Task<PlatformMaintenanceWindow?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Windows.FirstOrDefault(item => item.Id == id));
    }

    public Task<IReadOnlyList<PlatformMaintenanceWindow>> ListOpenAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformMaintenanceWindow>>(Windows.Where(item => item.CancelledUtc == null && item.EndsUtc > utcNow).OrderBy(item => item.StartsUtc).ToList());
    }

    public Task<PagedResult<PlatformMaintenanceWindow>> ListPastAsync(DateTimeOffset utcNow, PagedRequest page, CancellationToken cancellationToken)
    {
        List<PlatformMaintenanceWindow> past = Windows.Where(item => item.CancelledUtc != null || item.EndsUtc <= utcNow).ToList();
        return Task.FromResult(new PagedResult<PlatformMaintenanceWindow>(past, page, past.Count));
    }

    public Task<PlatformMaintenanceWindow?> FindByPassHashAsync(string passHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Windows.FirstOrDefault(item => item.PassHash == passHash));
    }
}
