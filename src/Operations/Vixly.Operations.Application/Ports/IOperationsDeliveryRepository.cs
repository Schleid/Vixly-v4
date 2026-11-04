using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDeliveryRepository.cs
//
// PURPOSE:
// Reads and writes delivery routes and their stops (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Routes, stops, the shipments waiting for delivery at a warehouse, the parcels going back
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter
// =========================================================
public interface IOperationsDeliveryRepository
{
    void Add(OperationsDeliveryRoute route);

    void AddStop(OperationsDeliveryStop stop);

    void RemoveStop(OperationsDeliveryStop stop);

    // Tracked.
    Task<OperationsDeliveryRoute?> GetAsync(Guid routeId, CancellationToken cancellationToken);

    Task<OperationsDeliveryRoute?> FindByIdAsync(Guid routeId, CancellationToken cancellationToken);

    Task<OperationsDeliveryRoute?> FindByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    // Tracked, in their order.
    Task<IReadOnlyList<OperationsDeliveryStop>> ListStopsAsync(Guid routeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsDeliveryStop>> ListStopsAsync(IReadOnlyCollection<Guid> routeIds, CancellationToken cancellationToken);

    // Planned, out or back from this day on, and the latest others; newest first.
    Task<IReadOnlyList<OperationsDeliveryRoute>> ListRoutesAsync(Guid warehouseId, DateOnly from, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsDeliveryRoute>> ListDriverRoutesAsync(Guid userId, DateOnly from, int take, CancellationToken cancellationToken);

    // The parcels at the warehouse arrived for home delivery and on no route, with their shipments (not tracked).
    Task<IReadOnlyList<(OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels)>> ListWaitingAsync(Guid warehouseId, int take, CancellationToken cancellationToken);

    // Tracked.
    Task<IReadOnlyList<OperationsParcel>> ListRouteParcelsAsync(Guid routeId, CancellationToken cancellationToken);

    // Tracked.
    Task<IReadOnlyList<OperationsShipment>> ListShipmentsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // The invoices of these shipments collected for at the door on routes not settled yet: they still show owed until paid in (review).
    Task<IReadOnlySet<Guid>> CollectedAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // Read again from the database, not tracked (a double click).
    Task<OperationsDeliveryStop?> FindStopAsync(Guid stopId, CancellationToken cancellationToken);

    Task<IReadOnlyList<(OperationsParcel Parcel, OperationsShipment Shipment)>> ListReturnsAsync(Guid warehouseId, int take, CancellationToken cancellationToken);
}
