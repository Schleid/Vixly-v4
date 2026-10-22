using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsBundleRepository.cs
//
// PURPOSE:
// Reads and writes bundles and repack boxes (Slice 6A).
//
// RESPONSIBILITIES:
// - Add them; find them by id, number or form key
// - List the parcels ready to bundle, a bundle's and a box's parcels
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); lists are bounded
// =========================================================
public interface IOperationsBundleRepository
{
    void Add(OperationsBundle bundle);

    void Add(OperationsRepackBox box);

    // The bundle's list (owner, 2026-10-21).
    void Add(OperationsBundleItem item);

    void Remove(OperationsBundleItem item);

    Task<bool> HasListAsync(Guid bundleId, CancellationToken cancellationToken);

    Task<bool> AnyMissingAsync(Guid bundleId, CancellationToken cancellationToken);

    // Tracked.
    Task<IReadOnlyList<OperationsBundleItem>> ListItemsAsync(Guid bundleId, CancellationToken cancellationToken);

    // Tracked: the list lines of open bundles holding these parcels or boxes.
    Task<IReadOnlyList<OperationsBundleItem>> ListOpenItemsAsync(IReadOnlyCollection<Guid> parcelIds, IReadOnlyCollection<Guid> boxIds, CancellationToken cancellationToken);

    // The parcels and boxes on the lists of open bundles at these warehouses, not scanned yet.
    // Each line's parcel or box number and its shipment's number.
    Task<IReadOnlyDictionary<Guid, (string Number, string? ShipmentNumber)>> NameItemsAsync(IReadOnlyCollection<OperationsBundleItem> items, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsBundleItem>> ListPlannedAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken);

    // Tracked.
    Task<OperationsBundle?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken);

    Task<OperationsBundle?> FindBundleByNumberAsync(string number, CancellationToken cancellationToken);

    Task<OperationsBundle?> FindBundleByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    // Read only, newest first: the open and closed bundles in these warehouses (voided ones of the last days too).
    Task<IReadOnlyList<OperationsBundle>> ListBundlesAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsBundle>> ListBundlesByIdAsync(IReadOnlyCollection<Guid> bundleIds, CancellationToken cancellationToken);

    // Tracked: the parcels in a bundle.
    Task<IReadOnlyList<OperationsParcel>> ListBundleParcelsForUpdateAsync(Guid bundleId, CancellationToken cancellationToken);

    // Read only: parcels of final shipments still at these warehouses and in no bundle, with their shipment (D-10, ready to bundle).
    Task<IReadOnlyList<(OperationsParcel Parcel, OperationsShipment Shipment)>> ListReadyAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);

    // Tracked.
    Task<OperationsRepackBox?> GetBoxAsync(Guid boxId, CancellationToken cancellationToken);

    Task<OperationsRepackBox?> FindBoxByNumberAsync(string number, CancellationToken cancellationToken);

    Task<OperationsRepackBox?> FindBoxByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    // Tracked: the shipment's box that is not voided.
    Task<OperationsRepackBox?> FindLiveBoxAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Read only: the boxes not voided in these warehouses, or in a bundle.
    Task<IReadOnlyList<OperationsRepackBox>> ListBoxesAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRepackBox>> ListBundleBoxesForUpdateAsync(Guid bundleId, CancellationToken cancellationToken);
}
