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
// - Owner, 2026-10-28 (option A): only once the shipment has its tracking number (paid, on account or collect); the label is the proof that it may move on
// - Owner, 2026-10-31: the service's label code (the company's, else the catalogue's) and the ship date in the company's time zone
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

    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly TimeProvider _timeProvider;

    public OperationsShippingLabels(IOperationsReceivingRepository receiving, IOperationsLocationRepository locations, IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands, OperationsNumbering numbering, IOperationsSiteRepository site, IOperationsShippingServiceRepository services,
        TimeProvider timeProvider)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _site = site ?? throw new ArgumentNullException(nameof(site));
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

    // Why a parcel has no shipping label: not cleared yet, or cleared but its shipment is not paid yet.
    public async Task<Vixly.Shared.Kernel.Errors.Error> RefusalAsync(OperationsParcel parcel, CancellationToken cancellationToken) =>
        await ClearedShipmentAsync(parcel, cancellationToken) is { TrackingNumber: null }
            ? Vixly.Operations.Domain.Printing.OperationsPrintingErrors.NotPaid
            : Vixly.Operations.Domain.Printing.OperationsPrintingErrors.NotCleared;

    public async Task<OperationsShippingLabel?> BuildAsync(OperationsParcel parcel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (parcel.Number is not { } number || await ClearedShipmentAsync(parcel, cancellationToken) is not { TrackingNumber: not null } shipment)
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
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsWeightUnit unit = settings?.WeightUnit ?? OperationsWeightUnit.Kilogram;

        // Owner, 2026-10-28: the black and white copy of the document logo, for labels sent to a thermal printer.
        byte[]? logo = settings?.LabelLogoImageId is { } logoId ? (await _site.GetImageAsync(logoId, cancellationToken))?.Content : null;
        string weight = parcel.WeightKg is { } kg
            ? OperationsUnits.ToShownWeight(kg, unit).ToString("0.##", CultureInfo.InvariantCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg")
            : string.Empty;

        Vixly.Contracts.Platform.Catalog.PlatformShippingServiceInfo? service = Vixly.Contracts.Platform.Catalog.PlatformShippingServices.Find(shipment.ServiceCode);
        OperationsShippingServiceSetup? setup = service is null ? null : (await _services.ListAsync(cancellationToken)).FirstOrDefault(item => item.ServiceCode == service.Code);
        TimeZoneInfo zone = TimeZoneInfo.TryFindSystemTimeZoneById(settings?.TimeZone ?? "UTC", out TimeZoneInfo? found) ? found : TimeZoneInfo.Utc;
        string shipDate = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Vixly.Operations.Domain.Locations.OperationsWarehouse? origin = warehouses.GetValueOrDefault(shipment.OriginWarehouseId);

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
            shipment.DeliveryAddress?.City ?? destination?.Address.City ?? branch?.Address?.City ?? string.Empty,
            shipment.DeliveryAddress is null,
            weight,
            shipment.TrackingNumber,
            logo,
            service is null ? null : setup?.LabelCode ?? service.DefaultLabelCode,
            service is null ? null : setup?.Label ?? ServiceName(service.Code),
            service?.Mode,
            service?.Code == Vixly.Contracts.Platform.Catalog.PlatformShippingServices.AirExpress,
            shipDate,
            origin?.Name,
            shipment.Options.HasFlag(OperationsShipmentOptions.Insured),
            shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate));
    }

    // The catalogue code as words ("air_standard": "Air standard") when the company gave the service no label of its own.
    private static string ServiceName(string code)
    {
        string words = code.Replace('_', ' ');
        return words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
