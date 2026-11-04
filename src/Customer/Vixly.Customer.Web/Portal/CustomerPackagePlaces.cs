using Vixly.Contracts.Operations.Packages;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerPackagePlaces.cs
//
// PURPOSE:
// Which of the customer's shipments a package can go into, and which packages are in none (Slice 4D; owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - The open shipments with instructions from a warehouse
// - The pre-alerts and parcels still at their warehouse that are in none of the customer's shipments
//
// IMPORTANT NOTES:
// - Only for showing choices; Operations decides
// =========================================================
public static class CustomerPackagePlaces
{
    public static IReadOnlyList<CustomerShipmentDto> Open(CustomerPackagesDto? packages, Guid? warehouseId) =>
        (packages?.Shipments ?? Array.Empty<CustomerShipmentDto>())
            .Where(shipment => shipment.IsOpen && shipment.HasInstructions && (warehouseId is null || shipment.OriginId == warehouseId))
            .OrderByDescending(shipment => shipment.CreatedUtc).ToList();

    // A parcel's shipment: a real one, or none when it waits in the waiting shipment.
    public static Guid? ShipmentOf(CustomerPackagesDto? packages, CustomerParcelDto parcel) =>
        Find(packages, parcel.ShipmentId) is { HasInstructions: true } shipment ? shipment.ShipmentId : null;

    public static Guid? WarehouseOf(CustomerPackagesDto? packages, CustomerParcelDto parcel) => Find(packages, parcel.ShipmentId)?.OriginId;

    // Still at its warehouse: received, not yet past the origin.
    public static bool Movable(CustomerParcelDto parcel) => parcel.Milestone is null or "PreAlerted" or "ReceivedAtOrigin";

    public static IReadOnlyList<CustomerPreAlertDto> UnplacedPreAlerts(CustomerPackagesDto? packages, Guid? warehouseId) =>
        (packages?.PreAlerts ?? Array.Empty<CustomerPreAlertDto>()).Where(preAlert => preAlert.ShipmentId is null && (warehouseId is null || preAlert.WarehouseId == warehouseId)).ToList();

    public static IReadOnlyList<CustomerParcelDto> UnplacedParcels(CustomerPackagesDto? packages, Guid? warehouseId) =>
        (packages?.Parcels ?? Array.Empty<CustomerParcelDto>())
            .Where(parcel => Movable(parcel) && ShipmentOf(packages, parcel) is null && (warehouseId is null || WarehouseOf(packages, parcel) == warehouseId)).ToList();

    private static CustomerShipmentDto? Find(CustomerPackagesDto? packages, Guid? shipmentId) =>
        shipmentId is { } id ? packages?.Shipments?.FirstOrDefault(shipment => shipment.ShipmentId == id) : null;
}
