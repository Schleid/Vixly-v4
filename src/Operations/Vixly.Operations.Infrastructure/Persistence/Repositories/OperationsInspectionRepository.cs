using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsInspectionRepository.cs
//
// PURPOSE:
// Reads and writes the restricted list, inspection rules and inspections (Slice 3C).
//
// RESPONSIBILITIES:
// - Implement IOperationsInspectionRepository over the Operations database
//
// IMPORTANT NOTES:
// - Every query runs inside the tenant filter (D-F1)
// - The queue and the holds page are paged and sorted by the database
// =========================================================
internal sealed class OperationsInspectionRepository : IOperationsInspectionRepository
{
    private readonly OperationsDbContext _context;

    public OperationsInspectionRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsRestrictedItem item) => _context.RestrictedItems.Add(item ?? throw new ArgumentNullException(nameof(item)));

    public void Add(OperationsInspectionRule rule) => _context.InspectionRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void Add(OperationsInspection inspection) => _context.Inspections.Add(inspection ?? throw new ArgumentNullException(nameof(inspection)));

    public async Task<IReadOnlyList<OperationsRestrictedItem>> ListItemsAsync(CancellationToken cancellationToken) =>
        await _context.RestrictedItems.AsNoTracking().OrderBy(item => item.Name).Take(OperationsInspectionLimits.MaxItems).ToListAsync(cancellationToken);

    public Task<OperationsRestrictedItem?> GetItemAsync(Guid itemId, CancellationToken cancellationToken) =>
        _context.RestrictedItems.FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public Task<OperationsRestrictedItem?> FindItemByNameAsync(string name, CancellationToken cancellationToken) =>
        _context.RestrictedItems.AsNoTracking().FirstOrDefaultAsync(item => item.Name == name, cancellationToken);

    public async Task<IReadOnlyList<OperationsInspectionRule>> ListRulesAsync(CancellationToken cancellationToken) =>
        await _context.InspectionRules.AsNoTracking().OrderBy(rule => rule.Kind).ThenBy(rule => rule.CreatedUtc).Take(OperationsInspectionLimits.MaxRules).ToListAsync(cancellationToken);

    public Task<OperationsInspectionRule?> GetRuleAsync(Guid ruleId, CancellationToken cancellationToken) =>
        _context.InspectionRules.FirstOrDefaultAsync(rule => rule.Id == ruleId, cancellationToken);

    public Task<OperationsInspection?> GetInspectionAsync(Guid inspectionId, CancellationToken cancellationToken) =>
        _context.Inspections.FirstOrDefaultAsync(item => item.Id == inspectionId, cancellationToken);

    public async Task<OperationsInspection?> FindLatestAsync(Guid parcelId, CancellationToken cancellationToken) =>
        _context.Inspections.Local.Where(item => item.ParcelId == parcelId).OrderByDescending(item => item.CreatedUtc).FirstOrDefault()
        ?? await _context.Inspections.Where(item => item.ParcelId == parcelId).OrderByDescending(item => item.CreatedUtc).ThenByDescending(item => item.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsInspection>> ListLatestAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken)
    {
        if (parcelIds.Count == 0)
        {
            return Array.Empty<OperationsInspection>();
        }

        List<Guid> ids = parcelIds.Distinct().ToList();
        List<OperationsInspection> all = await _context.Inspections.AsNoTracking().Where(item => ids.Contains(item.ParcelId)).ToListAsync(cancellationToken);
        return all.GroupBy(item => item.ParcelId).Select(group => group.OrderByDescending(item => item.CreatedUtc).First()).ToList();
    }

    public async Task<(IReadOnlyList<OperationsInspection> Items, int TotalCount)> ListQueueAsync(IReadOnlyCollection<Guid> warehouseIds, int page, int pageSize, CancellationToken cancellationToken,
        Guid? parcelId = null)
    {
        if (warehouseIds.Count == 0)
        {
            return (Array.Empty<OperationsInspection>(), 0);
        }

        List<Guid> warehouses = warehouseIds.ToList();
        IQueryable<OperationsInspection> query = _context.Inspections.AsNoTracking()
            .Where(item => warehouses.Contains(item.WarehouseId) && (item.Status == OperationsInspectionStatus.Waiting || item.Status == OperationsInspectionStatus.InProgress));

        if (parcelId is { } parcel)
        {
            query = query.Where(item => item.ParcelId == parcel);
        }
        int total = await query.CountAsync(cancellationToken);
        List<OperationsInspection> items = await query.OrderBy(item => item.CreatedUtc).ThenBy(item => item.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListParcelsAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken)
    {
        List<Guid> ids = parcelIds.Distinct().ToList();
        return ids.Count == 0 ? Array.Empty<OperationsParcel>() : await _context.Parcels.AsNoTracking().Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsInspection>> ListAwaitingApprovalAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken)
    {
        List<Guid> warehouses = warehouseIds.ToList();
        return warehouses.Count == 0
            ? Array.Empty<OperationsInspection>()
            : await _context.Inspections.AsNoTracking().Where(item => warehouses.Contains(item.WarehouseId) && item.Status == OperationsInspectionStatus.AwaitingApproval)
                .OrderBy(item => item.FinishedUtc).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OperationsParcel> Items, int TotalCount)> ListHeldAsync(IReadOnlyCollection<Guid> warehouseIds, OperationsHoldReasons reason, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        if (warehouseIds.Count == 0)
        {
            return (Array.Empty<OperationsParcel>(), 0);
        }

        List<Guid> warehouses = warehouseIds.ToList();
        IQueryable<OperationsParcel> query = _context.Parcels.AsNoTracking().Where(item => item.WarehouseId != null && warehouses.Contains(item.WarehouseId.Value));
        query = reason == OperationsHoldReasons.None
            ? query.Where(item => item.HoldReasons != OperationsHoldReasons.None)
            : query.Where(item => (item.HoldReasons & reason) != OperationsHoldReasons.None);
        int total = await query.CountAsync(cancellationToken);
        List<OperationsParcel> items = await query.OrderBy(item => item.LastEventUtc).ThenBy(item => item.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
