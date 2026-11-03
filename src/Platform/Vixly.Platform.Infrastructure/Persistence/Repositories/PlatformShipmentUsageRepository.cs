using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformShipmentUsageRepository.cs
//
// PURPOSE:
// Loads and stores the monthly shipment counts (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Find one month, list each company's latest month, add a month
//
// IMPORTANT NOTES:
// - "Latest" is the highest month string ("2026-10" sorts as a date)
// =========================================================
internal sealed class PlatformShipmentUsageRepository : IPlatformShipmentUsageRepository
{
    private readonly PlatformDbContext _context;

    public PlatformShipmentUsageRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformTenantShipmentUsage?> GetAsync(Guid platformTenantId, string month, CancellationToken cancellationToken)
    {
        return _context.TenantShipmentUsage.FirstOrDefaultAsync(item => item.PlatformTenantId == platformTenantId && item.Month == month, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformTenantShipmentUsage>> ListLatestAsync(IReadOnlyCollection<Guid> platformTenantIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(platformTenantIds);

        if (platformTenantIds.Count == 0)
        {
            return Array.Empty<PlatformTenantShipmentUsage>();
        }

        List<PlatformTenantShipmentUsage> rows = await _context.TenantShipmentUsage.AsNoTracking()
            .Where(item => platformTenantIds.Contains(item.PlatformTenantId))
            .Where(item => item.Month == _context.TenantShipmentUsage
                .Where(other => other.PlatformTenantId == item.PlatformTenantId)
                .Max(other => other.Month))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public void Add(PlatformTenantShipmentUsage usage)
    {
        ArgumentNullException.ThrowIfNull(usage);

        _context.TenantShipmentUsage.Add(usage);
    }
}
