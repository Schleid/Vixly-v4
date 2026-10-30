using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsArrivalRepository.cs
//
// PURPOSE:
// EF Core access for destination receiving (Slice 7 step 1).
//
// RESPONSIBILITIES:
// - Dispatches bound for a country, discrepancies, parcels and boxes scanned
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter
// - Lists that feed a save are tracked; the others are read-only
// =========================================================
internal sealed class OperationsArrivalRepository : IOperationsArrivalRepository
{
    private readonly OperationsDbContext _context;

    public OperationsArrivalRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<OperationsDispatch>> ListArrivingAsync(Guid warehouseId, string countryCode, bool received, int take, CancellationToken cancellationToken)
    {
        IQueryable<OperationsDispatch> dispatches = _context.Dispatches.AsNoTracking()
            .Where(item => (item.Status == OperationsDispatchStatus.Departed || item.Status == OperationsDispatchStatus.Arrived) && item.WarehouseId != warehouseId
                && (item.ReceivingWarehouseId == warehouseId
                    || (item.ReceivingWarehouseId == null && item.DestinationWarehouseId == null && item.DestinationCountryCode == countryCode)));

        return received
            ? await dispatches.Where(item => item.ReceivedUtc != null).OrderByDescending(item => item.ReceivedUtc).Take(take).ToListAsync(cancellationToken)
            : await dispatches.Where(item => item.ReceivedUtc == null).OrderBy(item => item.DepartedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public void Add(OperationsArrivalDiscrepancy discrepancy) => _context.ArrivalDiscrepancies.Add(discrepancy ?? throw new ArgumentNullException(nameof(discrepancy)));

    public Task<OperationsArrivalDiscrepancy?> GetDiscrepancyAsync(Guid discrepancyId, CancellationToken cancellationToken) =>
        _context.ArrivalDiscrepancies.FirstOrDefaultAsync(item => item.Id == discrepancyId, cancellationToken);

    public async Task<IReadOnlyList<OperationsArrivalDiscrepancy>> ListOpenForAsync(IReadOnlyCollection<Guid> bundleIds, IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bundleIds);
        ArgumentNullException.ThrowIfNull(parcelIds);

        if (bundleIds.Count == 0 && parcelIds.Count == 0)
        {
            return Array.Empty<OperationsArrivalDiscrepancy>();
        }

        List<OperationsArrivalDiscrepancy> stored = await _context.ArrivalDiscrepancies.Where(item => item.Status == OperationsDiscrepancyStatus.Open
            && ((item.ParcelId == null && item.BundleId != null && bundleIds.Contains(item.BundleId.Value)) || (item.ParcelId != null && parcelIds.Contains(item.ParcelId.Value))))
            .ToListAsync(cancellationToken);

        // Added in this unit of work and not saved yet: a second note in the same scan is not written twice.
        return stored.Concat(_context.ArrivalDiscrepancies.Local.Where(item => item.IsOpen
            && ((item.ParcelId is null && item.BundleId is { } bundle && bundleIds.Contains(bundle)) || (item.ParcelId is { } parcel && parcelIds.Contains(parcel)))))
            .DistinctBy(item => item.Id).ToList();
    }

    public async Task<IReadOnlyList<OperationsArrivalDiscrepancy>> ListDispatchDiscrepanciesAsync(Guid dispatchId, CancellationToken cancellationToken) =>
        await _context.ArrivalDiscrepancies.AsNoTracking().Where(item => item.DispatchId == dispatchId).OrderBy(item => item.CreatedUtc).Take(500).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(OperationsArrivalDiscrepancy Item, string? DispatchNumber, Guid? OriginWarehouseId)>> ListDiscrepanciesAsync(IReadOnlyCollection<Guid> warehouseIds,
        bool openOnly, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        var rows = await (from item in _context.ArrivalDiscrepancies.AsNoTracking()
                          join dispatch in _context.Dispatches.AsNoTracking() on item.DispatchId equals dispatch.Id into found
                          from dispatch in found.DefaultIfEmpty()
                          where (warehouseIds.Contains(item.WarehouseId) || (dispatch != null && warehouseIds.Contains(dispatch.WarehouseId)))
                              && (!openOnly || item.Status == OperationsDiscrepancyStatus.Open)
                          orderby item.CreatedUtc descending
                          select new { item, Number = dispatch == null ? null : dispatch.Number, Origin = dispatch == null ? (Guid?)null : dispatch.WarehouseId }).Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(row => (row.item, (string?)row.Number, row.Origin)).ToList();
    }

    public Task<OperationsParcel?> FindParcelForUpdateAsync(string number, CancellationToken cancellationToken) =>
        _context.Parcels.FirstOrDefaultAsync(item => item.Number == number, cancellationToken);

    public async Task<IReadOnlyList<OperationsParcel>> ListBoxParcelsForUpdateAsync(Guid boxId, CancellationToken cancellationToken) =>
        await _context.Parcels.Where(item => item.BoxId == boxId).Take(Vixly.Operations.Domain.Bundles.OperationsBundleLimits.MaxParcels).ToListAsync(cancellationToken);
}
