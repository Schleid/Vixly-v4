using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsStockCountRepository.cs
//
// PURPOSE:
// EF Core store of the counts of the undeclared parcels area (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Implement IOperationsStockCountRepository
//
// IMPORTANT NOTES:
// - The tenant query filter applies to every query
// =========================================================
internal sealed class OperationsStockCountRepository : IOperationsStockCountRepository
{
    private readonly OperationsDbContext _context;

    public OperationsStockCountRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsStockCount count) => _context.StockCounts.Add(count ?? throw new ArgumentNullException(nameof(count)));

    public Task<OperationsStockCount?> GetAsync(Guid countId, CancellationToken cancellationToken) =>
        _context.StockCounts.FirstOrDefaultAsync(item => item.Id == countId, cancellationToken);

    public Task<OperationsStockCount?> FindOpenAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        _context.StockCounts.FirstOrDefaultAsync(item => item.OpenWarehouseId == warehouseId, cancellationToken);

    public async Task<IReadOnlyList<OperationsStockCount>> ListAsync(IReadOnlyCollection<Guid> warehouses, int take, CancellationToken cancellationToken)
    {
        List<Guid> shown = warehouses.ToList();
        return await _context.StockCounts.AsNoTracking().Where(item => shown.Contains(item.WarehouseId)).OrderByDescending(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListUndeclaredAsync(Guid warehouseId, int take, CancellationToken cancellationToken) =>
        await _context.Parcels.AsNoTracking()
            .Where(item => item.WarehouseId == warehouseId && item.CustomerId == null && item.Milestone == OperationsMilestone.ReceivedAtOrigin
                && (item.Flags & OperationsParcelFlags.AwaitingCheck) == 0)
            .OrderBy(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);
}
