using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformMaintenanceRepository.cs
//
// PURPOSE:
// Stores maintenance windows.
//
// RESPONSIBILITIES:
// - Add, find, list open and past windows, find by pass hash
//
// IMPORTANT NOTES:
// - Open: not cancelled and ending after now, soonest first; past: the rest, newest first
// =========================================================
internal sealed class PlatformMaintenanceRepository : IPlatformMaintenanceRepository
{
    private readonly PlatformDbContext _context;

    public PlatformMaintenanceRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformMaintenanceWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _context.MaintenanceWindows.Add(window);
    }

    public Task<PlatformMaintenanceWindow?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.MaintenanceWindows.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformMaintenanceWindow>> ListOpenAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return await _context.MaintenanceWindows.AsNoTracking()
            .Where(item => item.CancelledUtc == null && item.EndsUtc > utcNow)
            .OrderBy(item => item.StartsUtc)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<PlatformMaintenanceWindow>> ListPastAsync(DateTimeOffset utcNow, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformMaintenanceWindow> past = _context.MaintenanceWindows.AsNoTracking()
            .Where(item => item.CancelledUtc != null || item.EndsUtc <= utcNow);

        int total = await past.CountAsync(cancellationToken);
        List<PlatformMaintenanceWindow> rows = await past
            .OrderByDescending(item => item.StartsUtc)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformMaintenanceWindow>(rows, page, total);
    }

    public Task<PlatformMaintenanceWindow?> FindByPassHashAsync(string passHash, CancellationToken cancellationToken)
    {
        return _context.MaintenanceWindows.AsNoTracking().FirstOrDefaultAsync(item => item.PassHash == passHash, cancellationToken);
    }
}
