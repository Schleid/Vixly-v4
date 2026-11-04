using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDispatchRepository.cs
//
// PURPOSE:
// Reads and writes carriers, their rates and dispatches (Slice 6B).
//
// RESPONSIBILITIES:
// - Find and list carriers, rates and dispatches
// - Find the bundles a dispatch can take, and those it holds
//
// IMPORTANT NOTES:
// - Tenant-scoped by the context's query filter
// =========================================================
public interface IOperationsDispatchRepository
{
    void Add(OperationsCarrier carrier);

    void Add(OperationsCarrierRate rate);

    void Add(OperationsDispatch dispatch);

    void Remove(OperationsCarrierRate rate);

    // Slice 7 (R-11): the bundles a dispatch carried, kept after they arrive and go on.
    void Add(OperationsDispatchBundle link);

    Task RemoveLinkAsync(Guid dispatchId, Guid bundleId, CancellationToken cancellationToken);

    // The dispatch's links, tracked: what arrived on it and what was missing.
    Task<IReadOnlyList<OperationsDispatchBundle>> ListLinksAsync(Guid dispatchId, CancellationToken cancellationToken);

    // Step 2: the shipments on a dispatch, with their house numbers and cost shares.
    void Add(OperationsDispatchShipment shipment);

    void Remove(OperationsDispatchShipment shipment);

    Task<IReadOnlyList<OperationsDispatchShipment>> ListDispatchShipmentsAsync(Guid dispatchId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCarrier>> ListCarriersAsync(CancellationToken cancellationToken);

    Task<OperationsCarrier?> GetCarrierAsync(Guid carrierId, CancellationToken cancellationToken);

    Task<bool> CarrierNameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCarrierRate>> ListRatesAsync(Guid carrierId, CancellationToken cancellationToken);

    Task<OperationsCarrierRate?> GetRateAsync(Guid rateId, CancellationToken cancellationToken);

    Task<OperationsDispatch?> GetDispatchAsync(Guid dispatchId, CancellationToken cancellationToken);

    Task<OperationsDispatch?> FindDispatchByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    // The bundles on a dispatch, tracked for update.
    Task<IReadOnlyList<OperationsBundle>> ListDispatchBundlesAsync(Guid dispatchId, CancellationToken cancellationToken);

    // Every parcel in the dispatch's bundles in one query: tracked (to record events) or read-only (for documents).
    Task<IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel>> ListDispatchParcelsAsync(Guid dispatchId, bool forUpdate, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel>> ListBundleParcelsAsync(Guid bundleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vixly.Operations.Domain.Shipments.OperationsShipment>> ListShipmentsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // Closed bundles of a warehouse, on no dispatch, that fit a destination and service.
    Task<IReadOnlyList<OperationsBundle>> ListFittingBundlesAsync(Guid warehouseId, Guid? destinationWarehouseId, string countryCode, string serviceCode, int take, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> CountFreeBundlesAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsDispatch>> ListDispatchesAsync(IReadOnlyCollection<Guid> warehouseIds, bool openOnly, int take, CancellationToken cancellationToken);

    // A bundle by its number, tracked for update.
    Task<OperationsBundle?> FindBundleForUpdateAsync(string number, CancellationToken cancellationToken);
}
