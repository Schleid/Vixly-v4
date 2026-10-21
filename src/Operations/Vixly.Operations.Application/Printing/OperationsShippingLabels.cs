using System.Globalization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsShippingLabels.cs
//
// PURPOSE:
// Builds the shipping label of a cleared parcel (Slice 4D step 2; owner, 2026-10-19, A-5).
//
// RESPONSIBILITIES:
// - Only for a cleared parcel: checked, no hold, no compliance inspection waiting, in one of the customer's shipments, still at its warehouse
// - The shipment gets its number (SH...) the first time; the piece is the parcel's place among the shipment's parcels
//
// IMPORTANT NOTES:
// - The caller saves (the number and the "printed" mark go with the print job)
// =========================================================
public sealed class OperationsShippingLabels
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly OperationsNumbering _numbering;

    public OperationsShippingLabels(IOperationsReceivingRepository receiving, IOperationsLocationRepository locations, IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands, OperationsNumbering numbering)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
    }

    // The parcel's shipment when the parcel is cleared; null otherwise.
    public async Task<OperationsShipment?> ClearedShipmentAsync(OperationsParcel parcel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        return parcel.ShipmentId is { } shipmentId && await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment && parcel.IsClearedIn(shipment) ? shipment : null;
    }

    public async Task<OperationsShippingLabel?> BuildAsync(OperationsParcel parcel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (parcel.Number is not { } number || await ClearedShipmentAsync(parcel, cancellationToken) is not { } shipment)
        {
            return null;
        }

        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));
        }

        List<Guid> parcels = (await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Select(item => item.Id).ToList();
        int piece = parcels.IndexOf(parcel.Id) is >= 0 and int index ? index + 1 : parcels.Count + 1;
        Dictionary<Guid, Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(item => item.Id);
        Vixly.Operations.Domain.Locations.OperationsBranch? branch = shipment.DestinationBranchId is { } branchId ? await _locations.GetBranchAsync(branchId, cancellationToken) : null;
        Vixly.Operations.Domain.Locations.OperationsWarehouse? destination = shipment.DestinationWarehouseId is { } destinationId ? warehouses.GetValueOrDefault(destinationId) : null;
        OperationsWeightUnit unit = (await _settings.FindAsync(cancellationToken))?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        string weight = parcel.WeightKg is { } kg
            ? OperationsUnits.ToShownWeight(kg, unit).ToString("0.##", CultureInfo.InvariantCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg")
            : string.Empty;

        return new OperationsShippingLabel(
            (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty,
            shipment.Number!,
            number,
            piece,
            Math.Max(parcels.Count, piece),
            warehouses.GetValueOrDefault(shipment.OriginWarehouseId)?.Code ?? string.Empty,
            destination?.Code ?? branch?.Code ?? shipment.DestinationCountryCode ?? string.Empty,
            shipment.ServiceCode,
            shipment.RecipientName ?? string.Empty,
            shipment.DeliveryAddress?.City ?? destination?.Address.City ?? branch?.Address.City ?? string.Empty,
            shipment.DeliveryAddress is null,
            weight);
    }
}
