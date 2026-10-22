using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsBundleRepository.cs
//
// PURPOSE:
// EF Core store of bundles and repack boxes (Slice 6A).
//
// RESPONSIBILITIES:
// - Implement IOperationsBundleRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1)
// =========================================================
internal sealed class OperationsBundleRepository : IOperationsBundleRepository
{
    private readonly OperationsDbContext _context;

    public OperationsBundleRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsBundle bundle) => _context.Bundles.Add(bundle ?? throw new ArgumentNullException(nameof(bundle)));

    public void Add(OperationsRepackBox box) => _context.RepackBoxes.Add(box ?? throw new ArgumentNullException(nameof(box)));

    public Task<OperationsBundle?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken) =>
        _context.Bundles.FirstOrDefaultAsync(bundle => bundle.Id == bundleId, cancellationToken);

    public Task<OperationsBundle?> FindBundleByNumberAsync(string number, CancellationToken cancellationToken) =>
        _context.Bundles.AsNoTracking().FirstOrDefaultAsync(bundle => bundle.Number == number, cancellationToken);

    public Task<OperationsBundle?> FindBundleByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.Bundles.AsNoTracking().FirstOrDefaultAsync(bundle => bundle.RequestKey == requestKey, cancellationToken);

    public async Task<IReadOnlyList<OperationsBundle>> ListBundlesAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-7);

        return await _context.Bundles.AsNoTracking()
            .Where(bundle => warehouseIds.Contains(bundle.WarehouseId) && (bundle.Status != OperationsBundleStatus.Voided || bundle.VoidedUtc > since))
            .OrderByDescending(bundle => bundle.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsBundle>> ListBundlesByIdAsync(IReadOnlyCollection<Guid> bundleIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bundleIds);
        return bundleIds.Count == 0 ? Array.Empty<OperationsBundle>() : await _context.Bundles.AsNoTracking().Where(bundle => bundleIds.Contains(bundle.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListBundleParcelsForUpdateAsync(Guid bundleId, CancellationToken cancellationToken) =>
        await _context.Parcels.Where(parcel => parcel.BundleId == bundleId).Take(OperationsBundleLimits.MaxParcels).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(OperationsParcel Parcel, OperationsShipment Shipment)>> ListReadyAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        var rows = await (from parcel in _context.Parcels.AsNoTracking()
                          join shipment in _context.Shipments.AsNoTracking() on parcel.ShipmentId equals shipment.Id
                          where parcel.BundleId == null && parcel.WarehouseId != null && warehouseIds.Contains(parcel.WarehouseId.Value)
                              && parcel.Milestone >= OperationsMilestone.Booked && parcel.Milestone < OperationsMilestone.Manifested && shipment.Status == OperationsShipmentStatus.Booked
                          orderby shipment.BookedUtc
                          select new { parcel, shipment }).Take(take).ToListAsync(cancellationToken);

        return rows.Select(row => (row.parcel, row.shipment)).ToList();
    }

    public Task<OperationsRepackBox?> GetBoxAsync(Guid boxId, CancellationToken cancellationToken) =>
        _context.RepackBoxes.FirstOrDefaultAsync(box => box.Id == boxId, cancellationToken);

    public Task<OperationsRepackBox?> FindBoxByNumberAsync(string number, CancellationToken cancellationToken) =>
        _context.RepackBoxes.FirstOrDefaultAsync(box => box.Number == number, cancellationToken);

    public Task<OperationsRepackBox?> FindBoxByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.RepackBoxes.AsNoTracking().FirstOrDefaultAsync(box => box.RequestKey == requestKey, cancellationToken);

    public Task<OperationsRepackBox?> FindLiveBoxAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _context.RepackBoxes.FirstOrDefaultAsync(box => box.ShipmentId == shipmentId && box.Status != OperationsRepackBoxStatus.Voided, cancellationToken);

    public async Task<IReadOnlyList<OperationsRepackBox>> ListBoxesAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        return await _context.RepackBoxes.AsNoTracking().Where(box => warehouseIds.Contains(box.WarehouseId) && box.Status != OperationsRepackBoxStatus.Voided)
            .OrderByDescending(box => box.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsRepackBox>> ListBundleBoxesForUpdateAsync(Guid bundleId, CancellationToken cancellationToken) =>
        await _context.RepackBoxes.Where(box => box.BundleId == bundleId).ToListAsync(cancellationToken);

    public void Add(OperationsBundleItem item) => _context.BundleItems.Add(item ?? throw new ArgumentNullException(nameof(item)));

    public void Remove(OperationsBundleItem item) => _context.BundleItems.Remove(item ?? throw new ArgumentNullException(nameof(item)));

    public Task<bool> HasListAsync(Guid bundleId, CancellationToken cancellationToken) => _context.BundleItems.AnyAsync(item => item.BundleId == bundleId, cancellationToken);

    public Task<bool> AnyMissingAsync(Guid bundleId, CancellationToken cancellationToken) =>
        _context.BundleItems.AnyAsync(item => item.BundleId == bundleId && item.ScannedUtc == null, cancellationToken);

    public async Task<IReadOnlyList<OperationsBundleItem>> ListItemsAsync(Guid bundleId, CancellationToken cancellationToken) =>
        await _context.BundleItems.Where(item => item.BundleId == bundleId).OrderBy(item => item.CreatedUtc).Take(2000).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsBundleItem>> ListOpenItemsAsync(IReadOnlyCollection<Guid> parcelIds, IReadOnlyCollection<Guid> boxIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcelIds);
        ArgumentNullException.ThrowIfNull(boxIds);

        if (parcelIds.Count == 0 && boxIds.Count == 0)
        {
            return Array.Empty<OperationsBundleItem>();
        }

        return await _context.BundleItems
            .Where(item => ((item.ParcelId != null && parcelIds.Contains(item.ParcelId.Value)) || (item.BoxId != null && boxIds.Contains(item.BoxId.Value)))
                && _context.Bundles.Any(bundle => bundle.Id == item.BundleId && bundle.Status == OperationsBundleStatus.Open))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, (string Number, string? ShipmentNumber)>> NameItemsAsync(IReadOnlyCollection<OperationsBundleItem> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        List<Guid> parcelIds = items.Where(item => item.ParcelId is not null).Select(item => item.ParcelId!.Value).ToList();
        List<Guid> boxIds = items.Where(item => item.BoxId is not null).Select(item => item.BoxId!.Value).ToList();
        List<Guid> shipmentIds = items.Select(item => item.ShipmentId).Distinct().ToList();
        Dictionary<Guid, string?> parcels = parcelIds.Count == 0 ? new() : await _context.Parcels.AsNoTracking().Where(parcel => parcelIds.Contains(parcel.Id))
            .ToDictionaryAsync(parcel => parcel.Id, parcel => parcel.Number, cancellationToken);
        Dictionary<Guid, string> boxes = boxIds.Count == 0 ? new() : await _context.RepackBoxes.AsNoTracking().Where(box => boxIds.Contains(box.Id))
            .ToDictionaryAsync(box => box.Id, box => box.Number, cancellationToken);
        Dictionary<Guid, string?> shipments = await _context.Shipments.AsNoTracking().Where(shipment => shipmentIds.Contains(shipment.Id))
            .ToDictionaryAsync(shipment => shipment.Id, shipment => shipment.Number, cancellationToken);
        return items.ToDictionary(item => item.Id, item => (
            item.ParcelId is { } parcel ? parcels.GetValueOrDefault(parcel) ?? string.Empty : item.BoxId is { } box ? boxes.GetValueOrDefault(box) ?? string.Empty : string.Empty,
            shipments.GetValueOrDefault(item.ShipmentId)));
    }

    public async Task<IReadOnlyList<OperationsBundleItem>> ListPlannedAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        return await _context.BundleItems.AsNoTracking()
            .Where(item => item.ScannedUtc == null
                && _context.Bundles.Any(bundle => bundle.Id == item.BundleId && bundle.Status == OperationsBundleStatus.Open && warehouseIds.Contains(bundle.WarehouseId)))
            .Take(5000).ToListAsync(cancellationToken);
    }
}
