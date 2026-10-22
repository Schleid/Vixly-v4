using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsReceivingRepository.cs
//
// PURPOSE:
// Stores pre-alerts, carrier deliveries, shipments, receipts, photos and storage locations (Slice 3B).
//
// RESPONSIBILITIES:
// - Add records; find the open pre-alert of a tracking number and a customer's open shipment
// - List and page parcels for staff and for their customer
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); records added in this unit of work are found too
// =========================================================
public interface IOperationsReceivingRepository
{
    void Add(OperationsPreAlert preAlert);

    void Add(OperationsShipment shipment);

    void Add(OperationsWarehouseReceipt receipt);

    void Add(OperationsParcelPhoto photo);

    void Add(OperationsStorageLocation location);

    void Add(OperationsInboundDelivery delivery);

    Task<OperationsInboundDelivery?> GetDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);

    Task<OperationsInboundDelivery?> FindDeliveryByOpenKeyAsync(string openKey, CancellationToken cancellationToken);

    // Open deliveries of these warehouses, newest first, with how many parcels each holds so far.
    Task<IReadOnlyList<(OperationsInboundDelivery Delivery, int Pieces)>> ListOpenDeliveriesAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OperationsWarehouseReceipt> Items, int TotalCount)> ListReceiptsAsync(IReadOnlyCollection<Guid> warehouseIds, int page, int pageSize, CancellationToken cancellationToken);

    Task<OperationsWarehouseReceipt?> GetReceiptAsync(Guid receiptId, CancellationToken cancellationToken);

    // The parcels of a delivery, in the order they were scanned.
    Task<IReadOnlyList<OperationsParcel>> ListDeliveryParcelsAsync(Guid deliveryId, CancellationToken cancellationToken);

    // A parcel by its ID label (PK…), whatever its warehouse; the caller checks the scope.
    Task<OperationsParcel?> FindParcelByNumberAsync(string number, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcel>> ListShipmentParcelsAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Slice 5C: the shipment's parcels as this unit of work sees them (changes and new parcels not saved yet included), to decide whether it is final.
    Task<IReadOnlyList<OperationsParcel>> ListShipmentParcelsForUpdateAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Slice 5C: how many parcels each shipment counts (not cancelled, returned or disposed of).
    Task<IReadOnlyDictionary<Guid, int>> CountShipmentParcelsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // The counter (owner, 2026-10-20): the customer's parcels still here and on hold.
    Task<int> CountCustomerParcelsOnHoldAsync(Guid customerId, CancellationToken cancellationToken);

    // Every customer's open pre-alert for this number: several customers may announce one number (review H1).
    Task<IReadOnlyList<OperationsPreAlert>> ListOpenPreAlertsByTrackingAsync(IReadOnlyCollection<string> trackingNumbers, CancellationToken cancellationToken);

    Task<OperationsPreAlert?> FindOpenPreAlertAsync(Guid customerId, string trackingNumber, CancellationToken cancellationToken);

    Task<OperationsPreAlert?> GetPreAlertAsync(Guid preAlertId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsPreAlert>> ListOpenPreAlertsAsync(Guid customerId, CancellationToken cancellationToken);

    Task<OperationsShipment?> FindOpenShipmentAsync(Guid customerId, Guid warehouseId, CancellationToken cancellationToken);

    Task<OperationsShipment?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Slice 4D step 2: cleared parcels of the warehouses whose shipping label was never printed, oldest first.
    Task<IReadOnlyList<OperationsParcel>> ListLabelsToPrintAsync(IReadOnlyCollection<Guid> warehouses, int take, CancellationToken cancellationToken);

    // Slice 4D: the shipment the customer's form created already (a double click).
    Task<OperationsShipment?> FindShipmentByKeyAsync(Guid customerId, string requestKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsShipment>> ListCustomerShipmentsAsync(Guid customerId, CancellationToken cancellationToken);

    // Tracked: the customer may withdraw them with their shipment.
    // Slice 4D: the incoming packages list (pre-alerts of the warehouses), newest expected first, and its counts.
    Task<(IReadOnlyList<Vixly.Operations.Application.Receiving.OperationsIncomingRow> Items, int TotalCount)> ListIncomingAsync(
        Vixly.Operations.Application.Receiving.OperationsIncomingFilter filter, int page, int pageSize, CancellationToken cancellationToken);

    Task<Vixly.Operations.Application.Receiving.OperationsIncomingCounts> CountIncomingAsync(Vixly.Operations.Application.Receiving.OperationsIncomingFilter filter, CancellationToken cancellationToken);

    // Slice 5A: the shipments desk, newest first; the counts beside its views.
    Task<(IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsShipmentRow> Items, int TotalCount)> ListShipmentsAsync(
        Vixly.Operations.Application.Shipments.OperationsShipmentFilter filter, int page, int pageSize, CancellationToken cancellationToken);

    Task<Vixly.Operations.Application.Shipments.OperationsShipmentCounts> CountShipmentsAsync(Vixly.Operations.Application.Shipments.OperationsShipmentFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsPreAlert>> ListShipmentPreAlertsAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Slice 5C: the shipment's pre-alerts as this unit of work sees them, announced or withdrawn and not yet saved included.
    Task<IReadOnlyList<OperationsPreAlert>> ListShipmentPreAlertsForUpdateAsync(Guid shipmentId, CancellationToken cancellationToken);

    // The receipt of the delivery the parcel came with, once that delivery is closed.
    Task<OperationsWarehouseReceipt?> FindReceiptAsync(Guid parcelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcelPhoto>> ListPhotosAsync(Guid parcelId, CancellationToken cancellationToken);

    Task<OperationsParcelPhoto?> GetPhotoAsync(Guid photoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsStorageLocation>> ListLocationsAsync(Guid warehouseId, CancellationToken cancellationToken);

    Task<OperationsStorageLocation?> GetLocationAsync(Guid locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsStorageLocation>> ListLocationsByIdAsync(IReadOnlyCollection<Guid> locationIds, CancellationToken cancellationToken);

    Task<bool> LocationCodeTakenAsync(Guid warehouseId, string code, Guid? exceptId, CancellationToken cancellationToken);

    // A parcel at this warehouse with this carrier tracking number that is still in the tenant's care.
    Task<OperationsParcel?> FindActiveParcelAsync(IReadOnlyCollection<Guid> warehouseIds, IReadOnlyCollection<string> carrierTrackingNumbers, CancellationToken cancellationToken);

    Task<OperationsParcel?> FindCustomerParcelByTrackingAsync(Guid customerId, string carrierTrackingNumber, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OperationsParcel> Items, int TotalCount)> ListParcelsAsync(OperationsParcelFilter filter, int page, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcel>> ListCustomerParcelsAsync(Guid customerId, CancellationToken cancellationToken);
}
