using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsDeliveryRepository.cs
//
// PURPOSE:
// EF Core access for home delivery (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Routes and their stops, the shipments waiting for delivery at a warehouse, the parcels going back
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter
// - The waiting parcels and those going back are tracked: planning and returning change them
// =========================================================
internal sealed class OperationsDeliveryRepository : IOperationsDeliveryRepository
{
    private const OperationsParcelFlags Stopped = OperationsParcelFlags.CustomsHeld | OperationsParcelFlags.ReturnPending;

    private readonly OperationsDbContext _context;

    public OperationsDeliveryRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsDeliveryRoute route) => _context.DeliveryRoutes.Add(route ?? throw new ArgumentNullException(nameof(route)));

    public void AddStop(OperationsDeliveryStop stop) => _context.DeliveryStops.Add(stop ?? throw new ArgumentNullException(nameof(stop)));

    public void RemoveStop(OperationsDeliveryStop stop) => _context.DeliveryStops.Remove(stop ?? throw new ArgumentNullException(nameof(stop)));

    public Task<OperationsDeliveryRoute?> GetAsync(Guid routeId, CancellationToken cancellationToken) =>
        _context.DeliveryRoutes.FirstOrDefaultAsync(route => route.Id == routeId, cancellationToken);

    public Task<OperationsDeliveryRoute?> FindByIdAsync(Guid routeId, CancellationToken cancellationToken) =>
        _context.DeliveryRoutes.AsNoTracking().FirstOrDefaultAsync(route => route.Id == routeId, cancellationToken);

    public Task<OperationsDeliveryRoute?> FindByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.DeliveryRoutes.AsNoTracking().FirstOrDefaultAsync(route => route.RequestKey == requestKey, cancellationToken);

    public async Task<IReadOnlyList<OperationsDeliveryStop>> ListStopsAsync(Guid routeId, CancellationToken cancellationToken) =>
        await _context.DeliveryStops.Where(stop => stop.RouteId == routeId).OrderBy(stop => stop.Sequence).Take(OperationsDeliveryRoute.MaxStops + 20).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsDeliveryStop>> ListStopsAsync(IReadOnlyCollection<Guid> routeIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(routeIds);
        return routeIds.Count == 0
            ? Array.Empty<OperationsDeliveryStop>()
            : await _context.DeliveryStops.AsNoTracking().Where(stop => routeIds.Contains(stop.RouteId)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsDeliveryRoute>> ListRoutesAsync(Guid warehouseId, DateOnly from, int take, CancellationToken cancellationToken) =>
        await _context.DeliveryRoutes.AsNoTracking()
            .Where(route => route.WarehouseId == warehouseId && (route.ServiceDate >= from || route.Status == OperationsRouteStatus.Out || route.Status == OperationsRouteStatus.Returned))
            .OrderByDescending(route => route.ServiceDate).ThenByDescending(route => route.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsDeliveryRoute>> ListDriverRoutesAsync(Guid userId, DateOnly from, int take, CancellationToken cancellationToken) =>
        await _context.DeliveryRoutes.AsNoTracking()
            .Where(route => route.DriverUserId == userId && route.Status != OperationsRouteStatus.Cancelled
                && (route.ServiceDate >= from || route.Status == OperationsRouteStatus.Out))
            .OrderBy(route => route.ServiceDate).ThenBy(route => route.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels)>> ListWaitingAsync(Guid warehouseId, int take,
        CancellationToken cancellationToken)
    {
        List<OperationsParcel> parcels = await _context.Parcels
            .Where(parcel => parcel.WarehouseId == warehouseId && parcel.Milestone == OperationsMilestone.ArrivedDestinationWarehouse && parcel.ShipmentId != null && parcel.RouteId == null
                && (parcel.Flags & Stopped) == 0
                && _context.Shipments.Any(shipment => shipment.Id == parcel.ShipmentId && shipment.Status == OperationsShipmentStatus.Booked && shipment.DeliveryAddress != null))
            .OrderBy(parcel => parcel.LastEventUtc).Take(take * 4).ToListAsync(cancellationToken);
        List<Guid> shipmentIds = parcels.Select(parcel => parcel.ShipmentId!.Value).Distinct().Take(take).ToList();
        Dictionary<Guid, OperationsShipment> shipments = await _context.Shipments.AsNoTracking().Where(shipment => shipmentIds.Contains(shipment.Id)).ToDictionaryAsync(shipment => shipment.Id,
            cancellationToken);
        return shipmentIds.Where(shipments.ContainsKey)
            .Select(id => (shipments[id], (IReadOnlyList<OperationsParcel>)parcels.Where(parcel => parcel.ShipmentId == id).ToList())).ToList();
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListRouteParcelsAsync(Guid routeId, CancellationToken cancellationToken) =>
        await _context.Parcels.Where(parcel => parcel.RouteId == routeId).Take(2000).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsShipment>> ListShipmentsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);
        return shipmentIds.Count == 0 ? Array.Empty<OperationsShipment>() : await _context.Shipments.Where(shipment => shipmentIds.Contains(shipment.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> CollectedAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);

        if (shipmentIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        List<string> lists = await _context.DeliveryStops.AsNoTracking()
            .Where(stop => shipmentIds.Contains(stop.ShipmentId) && stop.CollectedInvoices != null
                && _context.DeliveryRoutes.Any(route => route.Id == stop.RouteId && route.Status != OperationsRouteStatus.Settled))
            .Select(stop => stop.CollectedInvoices!).ToListAsync(cancellationToken);
        return lists.SelectMany(list => list.Split(',', StringSplitOptions.RemoveEmptyEntries)).Select(Guid.Parse).ToHashSet();
    }

    public Task<OperationsDeliveryStop?> FindStopAsync(Guid stopId, CancellationToken cancellationToken) =>
        _context.DeliveryStops.AsNoTracking().FirstOrDefaultAsync(stop => stop.Id == stopId, cancellationToken);

    public async Task<IReadOnlyList<(OperationsParcel Parcel, OperationsShipment Shipment)>> ListReturnsAsync(Guid warehouseId, int take, CancellationToken cancellationToken)
    {
        List<OperationsParcel> parcels = await _context.Parcels
            .Where(parcel => parcel.WarehouseId == warehouseId && parcel.ShipmentId != null && (parcel.Flags & OperationsParcelFlags.ReturnPending) != 0
                && parcel.Milestone != OperationsMilestone.ReturnedToSender && parcel.Milestone != OperationsMilestone.Cancelled && parcel.Milestone != OperationsMilestone.Disposed)
            .OrderBy(parcel => parcel.LastEventUtc).Take(take).ToListAsync(cancellationToken);
        List<Guid> shipmentIds = parcels.Select(parcel => parcel.ShipmentId!.Value).Distinct().ToList();
        Dictionary<Guid, OperationsShipment> shipments = await _context.Shipments.AsNoTracking().Where(shipment => shipmentIds.Contains(shipment.Id)).ToDictionaryAsync(shipment => shipment.Id,
            cancellationToken);
        return parcels.Where(parcel => shipments.ContainsKey(parcel.ShipmentId!.Value)).Select(parcel => (parcel, shipments[parcel.ShipmentId!.Value])).ToList();
    }
}
