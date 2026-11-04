using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupDesk.cs
//
// PURPOSE:
// What every pickup-counter use case shares (Slice 7 step 2, R-4 to R-6, R-9, R-10).
//
// RESPONSIBILITIES:
// - Open the counter for the warehouses where the person releases parcels
// - Load a shipment at a warehouse: its parcels, those ready there, its invoices and releases
//
// IMPORTANT NOTES:
// - A shipment with nothing at this warehouse is not found here
// - Ready means: ready for pickup, here, not held by customs
// =========================================================
public sealed class OperationsPickupDesk
{
    private readonly OperationsBundleDesk _bundles;
    private readonly IOperationsPickupRepository _pickups;
    private readonly IOperationsReceivingRepository _receiving;

    public OperationsPickupDesk(OperationsBundleDesk bundles, IOperationsPickupRepository pickups, IOperationsReceivingRepository receiving)
    {
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context,
        CancellationToken cancellationToken) =>
        _bundles.OpenAsync(context, new[] { OperationsPermissions.PickupRelease }, cancellationToken);

    public static bool IsReady(OperationsParcel parcel, OperationsWarehouse here) =>
        (parcel ?? throw new ArgumentNullException(nameof(parcel))).WarehouseId == (here ?? throw new ArgumentNullException(nameof(here))).Id
        && parcel.Milestone == OperationsMilestone.ReadyForPickup && !parcel.Flags.HasFlag(OperationsParcelFlags.CustomsHeld);

    // The shipment (tracked) with its parcels (tracked); none when nothing of it is or was here.
    public async Task<OperationsPickupShipment?> ShipmentAsync(Guid shipmentId, OperationsWarehouse here, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(here);

        if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is not { Status: OperationsShipmentStatus.Booked } shipment)
        {
            return null;
        }

        IReadOnlyList<OperationsParcel> parcels = await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken);

        if (parcels.All(parcel => parcel.WarehouseId != here.Id))
        {
            return null;
        }

        return new OperationsPickupShipment(shipment, parcels, parcels.Where(parcel => IsReady(parcel, here)).ToList(),
            await _pickups.ListInvoicesAsync(new[] { shipment.Id }, cancellationToken), await _pickups.ListReleasesAsync(shipment.Id, cancellationToken));
    }
}
