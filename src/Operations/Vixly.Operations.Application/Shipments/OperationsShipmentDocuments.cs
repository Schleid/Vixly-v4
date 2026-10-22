using System.Globalization;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentDocuments.cs
//
// PURPOSE:
// Builds the packing list and the pro forma commercial invoice of a shipment (Slice 5A, design section 3).
//
// RESPONSIBILITIES:
// - Gather the company, the shipper (the customer at its mailbox address), the consignee and one line per parcel
// - Number the shipment when its first document is made, as the shipping label does
//
// IMPORTANT NOTES:
// - Made on request from the current data, never stored
// - Only a shipment with instructions and at least one parcel has documents; the caller checks who may see it
// - Parcels returned, disposed of or sold (an end milestone) are not on the documents (review)
// =========================================================
public sealed class OperationsShipmentDocuments
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly OperationsNumbering _numbering;

    private readonly TimeProvider _timeProvider;

    public OperationsShipmentDocuments(IOperationsReceivingRepository receiving, IOperationsLocationRepository locations, IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands, IOperationsCustomerRepository customers, IOperationsShippingServiceRepository services, OperationsNumbering numbering, TimeProvider timeProvider)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // Null when the shipment has no documents (waiting, cancelled, or nothing in it yet).
    public async Task<OperationsShipmentDocument?> BuildAsync(OperationsShipment shipment, OperationsShipmentDocumentKind kind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (shipment.IsWaiting || shipment.Status == OperationsShipmentStatus.Cancelled
            || ShipmentParcels.Active(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)) is not { Count: > 0 } parcels
            || await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsWeightUnit weight = settings?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        OperationsDimensionUnit length = settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre;
        string currency = settings?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        OperationsWarehouse? origin = warehouses.FirstOrDefault(item => item.Id == shipment.OriginWarehouseId);
        OperationsWarehouse? destination = shipment.DestinationWarehouseId is { } destinationId ? warehouses.FirstOrDefault(item => item.Id == destinationId) : null;
        OperationsBranch? branch = shipment.DestinationBranchId is { } branchId ? await _locations.GetBranchAsync(branchId, cancellationToken) : null;

        // The shipper: the customer at its mailbox address at the origin warehouse.
        List<string> shipper = new();

        if (customer.CompanyName is { Length: > 0 } company)
        {
            shipper.Add(company);
        }

        if (origin is not null && OperationsMailboxAddresses.For(customer, new[] { origin }).FirstOrDefault() is { } mailbox)
        {
            shipper.Add(mailbox.Addressee);
            shipper.Add(mailbox.Line1 + (mailbox.Line2 is { Length: > 0 } line2 ? ", " + line2 : string.Empty));
            shipper.Add(string.Join(", ", new[] { mailbox.City, mailbox.Region, mailbox.PostalCode }.Where(part => !string.IsNullOrWhiteSpace(part))) + " " + mailbox.CountryCode);
        }

        else
        {
            shipper.Add(customer.FullName);
        }


        // The consignee: the recipient, at the delivery address or the pickup office.
        OperationsPostalAddress? to = shipment.DeliveryAddress ?? destination?.Address ?? branch?.Address;
        List<string> consignee = new() { shipment.RecipientName ?? string.Empty };

        if (shipment.DeliveryAddress is null && (destination?.Name ?? branch?.Name) is { } office)
        {
            consignee.Add(office);
        }

        if (to is not null)
        {
            consignee.Add(to.Line1 + (to.Line2 is { Length: > 0 } extra ? ", " + extra : string.Empty));
            consignee.Add(string.Join(", ", new[] { to.City, to.Region, to.PostalCode }.Where(part => !string.IsNullOrWhiteSpace(part))) + " " + to.CountryCode);
        }

        if (shipment.RecipientPhone is { Length: > 0 } phone)
        {
            consignee.Add(phone);
        }

        List<OperationsShipmentDocumentLine> lines = parcels.Select(parcel => new OperationsShipmentDocumentLine(
            parcel.Number ?? string.Empty,
            parcel.DeclaredContents ?? string.Empty,
            parcel.DeclaredValue,
            parcel.WeightKg is { } kg ? Weight(kg, weight) : null,
            parcel.LengthCm is { } l && parcel.WidthCm is { } w && parcel.HeightCm is { } h
                ? string.Join(" x ", new[] { l, w, h }.Select(side => decimal.Round(OperationsUnits.ToShownLength(side, length), 1).ToString("0.#", CultureInfo.InvariantCulture)))
                  + (length == OperationsDimensionUnit.Inch ? " in" : " cm")
                : null)).ToList();
        string? label = shipment.ServiceCode is { } code ? (await _services.ListAsync(cancellationToken)).FirstOrDefault(item => item.ServiceCode == code)?.Label : null;

        return new OperationsShipmentDocument(
            kind,
            (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty,
            shipment.Number!,
            OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings?.TimeZone),
            origin is null ? string.Empty : origin.Code + " · " + origin.Address.City + ", " + origin.Address.CountryCode,
            string.Join(" · ", new[] { destination?.Code ?? branch?.Code, string.Join(", ", new[] { to?.City, shipment.DestinationCountryCode }.Where(part => !string.IsNullOrWhiteSpace(part))) }
                .Where(part => !string.IsNullOrWhiteSpace(part))),
            shipment.ServiceCode,
            label,
            shipper,
            consignee,
            lines,
            parcels.Any(parcel => parcel.WeightKg is not null) ? Weight(parcels.Sum(parcel => parcel.WeightKg ?? 0m), weight) : "—",
            parcels.Sum(parcel => parcel.DeclaredValue ?? 0m),
            currency);
    }

    private static string Weight(decimal kilograms, OperationsWeightUnit unit) =>
        OperationsUnits.ToShownWeight(kilograms, unit).ToString("0.##", CultureInfo.InvariantCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg");
}
