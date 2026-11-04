using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsShipmentCounter.cs
//
// PURPOSE:
// Counts shipments for the plan's monthly cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Count the current company's shipments created in a period, cancelled ones left out
// - List the companies that created shipments since a moment (system scope)
//
// IMPORTANT NOTES:
// - Uses the (TenantId, CreatedUtc) index; the tenant filter applies as everywhere
// =========================================================
internal sealed class OperationsShipmentCounter : IOperationsShipmentCounter
{
    private readonly OperationsDbContext _context;

    public OperationsShipmentCounter(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> CountCreatedAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        _context.Shipments.AsNoTracking()
            .CountAsync(item => item.CreatedUtc >= fromUtc && item.CreatedUtc < toUtc && item.Status != OperationsShipmentStatus.Cancelled, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListTenantsCreatingSinceAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken) =>
        await _context.Shipments.AsNoTracking().Where(item => item.CreatedUtc >= sinceUtc).Select(item => item.TenantId).Distinct().OrderBy(id => id).Take(10_000).ToListAsync(cancellationToken);
}
