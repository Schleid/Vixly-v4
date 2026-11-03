using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsShelfCountRepository.cs
//
// PURPOSE:
// Shelf counts (Slice 12D).
//
// RESPONSIBILITIES:
// - Implement IOperationsShelfCountRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant scoping comes from the context's query filters
// =========================================================
internal sealed class OperationsShelfCountRepository : IOperationsShelfCountRepository
{
    private readonly OperationsDbContext _context;

    public OperationsShelfCountRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsShelfCount count) => _context.ShelfCounts.Add(count ?? throw new ArgumentNullException(nameof(count)));

    public Task<OperationsShelfCount?> GetAsync(Guid countId, CancellationToken cancellationToken) =>
        _context.ShelfCounts.FirstOrDefaultAsync(item => item.Id == countId, cancellationToken);

    public Task<OperationsShelfCount?> FindOpenAsync(Guid locationId, CancellationToken cancellationToken) =>
        _context.ShelfCounts.FirstOrDefaultAsync(item => item.LocationId == locationId && item.FinishedUtc == null, cancellationToken);

    public async Task<IReadOnlyList<OperationsShelfCount>> ListAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken) =>
        await _context.ShelfCounts.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId)).OrderByDescending(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);
}
