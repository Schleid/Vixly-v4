using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsDispatchRepository.cs
//
// PURPOSE:
// EF Core access to carriers, rates and dispatches (Slice 6B).
//
// RESPONSIBILITIES:
// - Implement IOperationsDispatchRepository
//
// IMPORTANT NOTES:
// - Tenant-scoped by the context's query filter
// =========================================================
internal sealed class OperationsDispatchRepository : IOperationsDispatchRepository
{
    private readonly OperationsDbContext _context;

    public OperationsDispatchRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsCarrier carrier) => _context.Carriers.Add(carrier ?? throw new ArgumentNullException(nameof(carrier)));

    public void Add(OperationsCarrierRate rate) => _context.CarrierRates.Add(rate ?? throw new ArgumentNullException(nameof(rate)));

    public void Add(OperationsDispatch dispatch) => _context.Dispatches.Add(dispatch ?? throw new ArgumentNullException(nameof(dispatch)));

    public void Remove(OperationsCarrierRate rate) => _context.CarrierRates.Remove(rate ?? throw new ArgumentNullException(nameof(rate)));

    public void Add(OperationsDispatchBundle link) => _context.DispatchBundles.Add(link ?? throw new ArgumentNullException(nameof(link)));

    public async Task RemoveLinkAsync(Guid dispatchId, Guid bundleId, CancellationToken cancellationToken)
    {
        foreach (OperationsDispatchBundle link in await _context.DispatchBundles.Where(item => item.DispatchId == dispatchId && item.BundleId == bundleId).ToListAsync(cancellationToken))
        {
            _context.DispatchBundles.Remove(link);
        }

        foreach (OperationsDispatchBundle added in _context.ChangeTracker.Entries<OperationsDispatchBundle>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.DispatchId == dispatchId && entry.Entity.BundleId == bundleId).Select(entry => entry.Entity).ToList())
        {
            _context.DispatchBundles.Remove(added);
        }
    }

    public async Task<IReadOnlyList<OperationsDispatchBundle>> ListLinksAsync(Guid dispatchId, CancellationToken cancellationToken) =>
        await _context.DispatchBundles.Where(link => link.DispatchId == dispatchId).ToListAsync(cancellationToken);

    public void Add(OperationsDispatchShipment shipment) => _context.DispatchShipments.Add(shipment ?? throw new ArgumentNullException(nameof(shipment)));

    public void Remove(OperationsDispatchShipment shipment) => _context.DispatchShipments.Remove(shipment ?? throw new ArgumentNullException(nameof(shipment)));

    // Added in this save (closing) or stored: both, so a dispatch closed and costed in one request shares its cost.
    public async Task<IReadOnlyList<OperationsDispatchShipment>> ListDispatchShipmentsAsync(Guid dispatchId, CancellationToken cancellationToken)
    {
        List<OperationsDispatchShipment> stored = await _context.DispatchShipments.Where(row => row.DispatchId == dispatchId).Take(5000).ToListAsync(cancellationToken);
        return stored.Concat(_context.ChangeTracker.Entries<OperationsDispatchShipment>().Where(entry => entry.State == EntityState.Added && entry.Entity.DispatchId == dispatchId)
            .Select(entry => entry.Entity).Where(row => !stored.Contains(row))).ToList();
    }

    public async Task<IReadOnlyList<OperationsCarrier>> ListCarriersAsync(CancellationToken cancellationToken) =>
        await _context.Carriers.AsNoTracking().OrderBy(carrier => carrier.Name).Take(500).ToListAsync(cancellationToken);

    public Task<OperationsCarrier?> GetCarrierAsync(Guid carrierId, CancellationToken cancellationToken) =>
        _context.Carriers.FirstOrDefaultAsync(carrier => carrier.Id == carrierId, cancellationToken);

    public Task<bool> CarrierNameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        _context.Carriers.AnyAsync(carrier => carrier.Name == name && carrier.Id != exceptId, cancellationToken);

    public async Task<IReadOnlyList<OperationsCarrierRate>> ListRatesAsync(Guid carrierId, CancellationToken cancellationToken) =>
        await _context.CarrierRates.AsNoTracking().Where(rate => rate.CarrierId == carrierId).Take(2000).ToListAsync(cancellationToken);

    public Task<OperationsCarrierRate?> GetRateAsync(Guid rateId, CancellationToken cancellationToken) =>
        _context.CarrierRates.FirstOrDefaultAsync(rate => rate.Id == rateId, cancellationToken);

    public Task<OperationsDispatch?> GetDispatchAsync(Guid dispatchId, CancellationToken cancellationToken) =>
        _context.Dispatches.FirstOrDefaultAsync(dispatch => dispatch.Id == dispatchId, cancellationToken);

    public Task<OperationsDispatch?> FindDispatchByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.Dispatches.AsNoTracking().FirstOrDefaultAsync(dispatch => dispatch.RequestKey == requestKey, cancellationToken);

    public async Task<IReadOnlyList<OperationsBundle>> ListDispatchBundlesAsync(Guid dispatchId, CancellationToken cancellationToken) =>
        await _context.Bundles.Where(bundle => _context.DispatchBundles.Any(link => link.DispatchId == dispatchId && link.BundleId == bundle.Id)).OrderBy(bundle => bundle.Number)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel>> ListDispatchParcelsAsync(Guid dispatchId, bool forUpdate, CancellationToken cancellationToken)
    {
        // No AsNoTracking inside: it would apply to the whole query and lose the parcels' new state.
        IQueryable<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = _context.Parcels
            .Where(parcel => _context.DispatchBundles.Any(link => link.DispatchId == dispatchId && link.BundleId == parcel.BundleId));
        return await (forUpdate ? parcels : parcels.AsNoTracking()).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel>> ListBundleParcelsAsync(Guid bundleId, CancellationToken cancellationToken) =>
        await _context.Parcels.AsNoTracking().Where(parcel => parcel.BundleId == bundleId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vixly.Operations.Domain.Shipments.OperationsShipment>> ListShipmentsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);
        List<Vixly.Operations.Domain.Shipments.OperationsShipment> found = new();

        foreach (Guid[] slice in shipmentIds.Distinct().Chunk(1000))
        {
            found.AddRange(await _context.Shipments.AsNoTracking().Where(shipment => slice.Contains(shipment.Id)).ToListAsync(cancellationToken));
        }

        return found;
    }

    public async Task<IReadOnlyList<OperationsBundle>> ListFittingBundlesAsync(Guid warehouseId, Guid? destinationWarehouseId, string countryCode, string serviceCode, int take,
        CancellationToken cancellationToken) =>
        await _context.Bundles.AsNoTracking().Where(bundle => (bundle.CurrentWarehouseId ?? bundle.WarehouseId) == warehouseId && bundle.Status == OperationsBundleStatus.Closed
                && bundle.DispatchId == null && bundle.OpenedUtc == null
                && (bundle.CurrentWarehouseId == null || (bundle.DestinationWarehouseId != null && bundle.DestinationWarehouseId != bundle.CurrentWarehouseId)) && bundle.DestinationWarehouseId == destinationWarehouseId && bundle.DestinationCountryCode == countryCode && bundle.ServiceCode == serviceCode)
            .OrderBy(bundle => bundle.ClosedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountFreeBundlesAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        return await _context.Bundles.AsNoTracking().Where(bundle => warehouseIds.Contains(bundle.CurrentWarehouseId ?? bundle.WarehouseId) && bundle.Status == OperationsBundleStatus.Closed
                && bundle.DispatchId == null && bundle.OpenedUtc == null
                && (bundle.CurrentWarehouseId == null || (bundle.DestinationWarehouseId != null && bundle.DestinationWarehouseId != bundle.CurrentWarehouseId)))
            .GroupBy(bundle => bundle.CurrentWarehouseId ?? bundle.WarehouseId).Select(group => new { group.Key, Count = group.Count() }).ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsDispatch>> ListDispatchesAsync(IReadOnlyCollection<Guid> warehouseIds, bool openOnly, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        return await _context.Dispatches.AsNoTracking().Where(dispatch => warehouseIds.Contains(dispatch.WarehouseId)
                && (!openOnly || (dispatch.Status != OperationsDispatchStatus.Arrived && dispatch.Status != OperationsDispatchStatus.Cancelled)))
            .OrderByDescending(dispatch => dispatch.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public Task<OperationsBundle?> FindBundleForUpdateAsync(string number, CancellationToken cancellationToken) =>
        _context.Bundles.FirstOrDefaultAsync(bundle => bundle.Number == number, cancellationToken);
}
