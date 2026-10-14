using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformLimitOverrideRepository.cs
//
// PURPOSE:
// Loads and stores limit overrides (D-S8).
//
// RESPONSIBILITIES:
// - List open overrides, find one, add one
//
// IMPORTANT NOTES:
// - Removed overrides stay as history
// =========================================================
internal sealed class PlatformLimitOverrideRepository : IPlatformLimitOverrideRepository
{
    private readonly PlatformDbContext _context;

    public PlatformLimitOverrideRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PlatformTenantLimitOverride>> ListOpenAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return await _context.TenantLimitOverrides
            .Where(item => item.PlatformTenantId == platformTenantId && item.RemovedUtc == null)
            .ToListAsync(cancellationToken);
    }

    public Task<PlatformTenantLimitOverride?> GetAsync(Guid overrideId, CancellationToken cancellationToken)
    {
        return _context.TenantLimitOverrides.FirstOrDefaultAsync(item => item.Id == overrideId, cancellationToken);
    }

    public void Add(PlatformTenantLimitOverride limitOverride)
    {
        ArgumentNullException.ThrowIfNull(limitOverride);

        _context.TenantLimitOverrides.Add(limitOverride);
    }
}
