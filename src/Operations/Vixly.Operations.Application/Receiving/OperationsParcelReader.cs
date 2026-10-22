using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelReader.cs
//
// PURPOSE:
// Builds the staff view of parcels (Slice 3B).
//
// RESPONSIBILITIES:
// - Add the customer, warehouse and shelf to a page of parcels with one read each
// - The instructions of a parcel's shipment, as the customer wrote them (owner, 2026-10-18)
//
// IMPORTANT NOTES:
// - Never one query per parcel
// =========================================================
public sealed class OperationsParcelReader
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsReceivingRepository _receiving;

    public OperationsParcelReader(IOperationsCustomerRepository customers, IOperationsLocationRepository locations, IOperationsReceivingRepository receiving)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public async Task<Vixly.Operations.Application.Shipments.OperationsCustomerShipment?> ShipmentAsync(Guid? shipmentId, int arrived, CancellationToken cancellationToken)
    {
        if (shipmentId is not { } id || await _receiving.GetShipmentAsync(id, cancellationToken) is not { } shipment)
        {
            return null;
        }

        string? origin = (await _locations.GetWarehouseAsync(shipment.OriginWarehouseId, cancellationToken))?.Name;
        // Slice 4C: picked up at the destination warehouse the customer chose; before 4C, at a destination branch.
        string? branch = shipment.DestinationWarehouseId is { } destinationId ? (await _locations.GetWarehouseAsync(destinationId, cancellationToken))?.Name
            : shipment.DestinationBranchId is { } branchId ? (await _locations.GetBranchAsync(branchId, cancellationToken))?.Name : null;
        return Vixly.Operations.Application.Shipments.OperationsCustomerShipment.Of(shipment, origin, branch, 0, arrived);
    }

    public async Task<IReadOnlyList<OperationsParcelSummary>> SummariesAsync(IReadOnlyList<OperationsParcel> parcels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);

        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(parcels.Where(parcel => parcel.CustomerId is not null).Select(parcel => parcel.CustomerId!.Value).Distinct().ToList(), cancellationToken))
            .ToDictionary(customer => customer.Id);
        Dictionary<Guid, OperationsWarehouse> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(warehouse => warehouse.Id);
        Dictionary<Guid, OperationsStorageLocation> shelves = (await _receiving.ListLocationsByIdAsync(parcels.Where(parcel => parcel.StorageLocationId is not null).Select(parcel => parcel.StorageLocationId!.Value).Distinct().ToList(), cancellationToken))
            .ToDictionary(location => location.Id);

        return parcels.Select(parcel =>
        {
            OperationsCustomer? customer = parcel.CustomerId is { } customerId ? customers.GetValueOrDefault(customerId) : null;

            return new OperationsParcelSummary(parcel.Id, parcel.Number, parcel.CarrierTrackingNumber, parcel.Carrier, parcel.CustomerId, customer?.Code, customer?.FullName,
                parcel.WarehouseId, parcel.WarehouseId is { } warehouseId ? warehouses.GetValueOrDefault(warehouseId)?.Code : null,
                parcel.WeightKg, parcel.LengthCm, parcel.WidthCm, parcel.HeightCm, parcel.DeclaredContents, parcel.DeclaredValue, parcel.CurrencyCode,
                parcel.Milestone, parcel.Flags, parcel.HoldReasons, parcel.CreatedUtc,
                parcel.StorageLocationId is { } shelfId ? shelves.GetValueOrDefault(shelfId)?.Code : null, parcel.ShippingLabelUtc,
                parcel.LabelName, parcel.LabelCode, parcel.LabelSender);
        }).ToList();
    }
}
