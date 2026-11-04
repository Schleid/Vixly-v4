using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsDispatchPapers.cs
//
// PURPOSE:
// The shipments of a closed dispatch and their papers (Slice 6B step 2, D-6): house numbers, house air waybills, the Shipper's Letter of Instruction.
//
// RESPONSIBILITIES:
// - At closing, write one row per shipment with its house number, pieces, weights and value
// - Build the house air waybills and the letter of instruction from the dispatch, its legs, its shipments and the company
//
// IMPORTANT NOTES:
// - Chargeable weight per parcel: the greater of actual and volumetric at its bundle's divisor (A-9)
// - The caller saves
// =========================================================
public sealed class OperationsDispatchPapers
{
    private readonly IOperationsDispatchRepository _dispatches;
    private readonly IOperationsCustomerRepository _customers;
    private readonly IOperationsLocationRepository _locations;

    public OperationsDispatchPapers(IOperationsDispatchRepository dispatches, IOperationsCustomerRepository customers, IOperationsLocationRepository locations)
    {
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public static decimal ChargeableKg(OperationsParcel parcel, decimal? divisor)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        decimal kg = parcel.WeightKg ?? 0m;
        return divisor is { } by and > 0m && parcel.LengthCm is { } l && parcel.WidthCm is { } w && parcel.HeightCm is { } h ? Math.Max(kg, decimal.Round(l * w * h / by, 3)) : kg;
    }

    // Closed: one row per shipment, numbered in the order of the shipments' numbers.
    public async Task ManifestAsync(OperationsDispatch dispatch, IReadOnlyList<OperationsParcel> all, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(all);
        Dictionary<Guid, decimal?> divisors = (await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken)).ToDictionary(bundle => bundle.Id, bundle => bundle.Divisor);
        List<(OperationsParcel Parcel, decimal? Divisor)> parcels = all.Select(parcel => (parcel, parcel.BundleId is { } id ? divisors.GetValueOrDefault(id) : null)).ToList();
        Dictionary<Guid, OperationsShipment> shipments = (await _dispatches.ListShipmentsAsync(parcels.Where(item => item.Parcel.ShipmentId is not null)
            .Select(item => item.Parcel.ShipmentId!.Value).Distinct().ToList(), cancellationToken)).ToDictionary(shipment => shipment.Id);
        List<(Guid ShipmentId, string Number, List<(OperationsParcel Parcel, decimal? Divisor)> Parcels)> groups = parcels.Where(item => item.Parcel.ShipmentId is not null)
            .GroupBy(item => item.Parcel.ShipmentId!.Value).Select(group => (group.Key, shipments.GetValueOrDefault(group.Key)?.Number ?? string.Empty, group.ToList())).ToList();

        int place = 0;

        foreach ((Guid shipmentId, string _, List<(OperationsParcel Parcel, decimal? Divisor)> items) in groups.OrderBy(item => item.Number, StringComparer.Ordinal))
        {
            _dispatches.Add(OperationsDispatchShipment.Create(dispatch.TenantId, dispatch.Id, shipmentId, OperationsDispatchShipment.House(dispatch.Number, ++place), items.Count,
                items.Sum(item => item.Parcel.WeightKg ?? 0m), items.Sum(item => ChargeableKg(item.Parcel, item.Divisor)), items.Sum(item => item.Parcel.DeclaredValue ?? 0m),
                items.Select(item => item.Parcel.CurrencyCode).FirstOrDefault(code => code is not null), utcNow));
        }
    }

    public async Task<IReadOnlyList<OperationsDispatchShipmentRow>> RowsAsync(OperationsDispatch dispatch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        IReadOnlyList<OperationsDispatchShipment> rows = await _dispatches.ListDispatchShipmentsAsync(dispatch.Id, cancellationToken);
        Dictionary<Guid, OperationsShipment> shipments = await ShipmentsAsync(rows, cancellationToken);
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(shipments.Values.Select(item => item.CustomerId).Distinct().ToList(), cancellationToken))
            .ToDictionary(customer => customer.Id);
        return rows.OrderBy(row => row.HouseNumber, StringComparer.Ordinal).Select(row =>
        {
            OperationsShipment? shipment = shipments.GetValueOrDefault(row.ShipmentId);
            OperationsCustomer? customer = shipment is null ? null : customers.GetValueOrDefault(shipment.CustomerId);
            return new OperationsDispatchShipmentRow(row, shipment?.Number ?? string.Empty, customer?.Code ?? string.Empty, shipment?.RecipientName ?? customer?.FullName ?? string.Empty);
        }).ToList();
    }

    // The house air waybills of the dispatch, or of one of its shipments.
    public async Task<IReadOnlyList<OperationsHouseWaybill>> HouseWaybillsAsync(OperationsDispatch dispatch, Guid? shipmentId, string companyName, IReadOnlyDictionary<Guid, OperationsCarrier> carriers,
        OperationsTenantSettings settings, DateOnly today, string contentsFallback, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(settings);
        IReadOnlyList<OperationsDispatchShipment> rows = (await _dispatches.ListDispatchShipmentsAsync(dispatch.Id, cancellationToken))
            .Where(row => shipmentId is null || row.ShipmentId == shipmentId).OrderBy(row => row.HouseNumber, StringComparer.Ordinal).ToList();
        Dictionary<Guid, OperationsShipment> shipments = await ShipmentsAsync(rows, cancellationToken);
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(shipments.Values.Select(item => item.CustomerId).Distinct().ToList(), cancellationToken))
            .ToDictionary(customer => customer.Id);
        Dictionary<Guid, OperationsWarehouse> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(warehouse => warehouse.Id);
        // Only the parcels on this dispatch: a shipment split across two flights declares on each what travels on it (review).
        ILookup<Guid?, OperationsParcel> contentsOf = (await _dispatches.ListDispatchParcelsAsync(dispatch.Id, false, cancellationToken)).ToLookup(parcel => parcel.ShipmentId);
        (string airFrom, string airTo) = dispatch.AirRoute();
        OperationsWarehouse? origin = warehouses.GetValueOrDefault(dispatch.WarehouseId);
        OperationsWaybillParty agent = new(companyName, origin is null ? Array.Empty<string>() : Lines(origin.Address), origin?.Phone);
        List<OperationsDispatchLeg> legs = dispatch.Legs.OrderBy(leg => leg.Sequence).ToList();
        List<string> routing = legs.Select(leg => leg.From + " → " + leg.To + (leg.CarrierId is { } id && carriers.GetValueOrDefault(id) is { } carrier ? " · " + carrier.Name : string.Empty)).ToList();
        List<OperationsHouseWaybill> waybills = new();

        foreach (OperationsDispatchShipment row in rows)
        {
            OperationsShipment? shipment = shipments.GetValueOrDefault(row.ShipmentId);
            OperationsCustomer? customer = shipment is null ? null : customers.GetValueOrDefault(shipment.CustomerId);
            OperationsWaybillParty shipper = new(customer is null ? string.Empty : customer.FullName + (customer.Code is { } code ? " · " + code : string.Empty),
                customer?.CompanyName is { } company ? new[] { company } : Array.Empty<string>(), customer?.Phone);
            OperationsWarehouse? pickup = shipment?.DestinationWarehouseId is { } pickupId ? warehouses.GetValueOrDefault(pickupId) : null;
            OperationsWaybillParty consignee = new(shipment?.RecipientName ?? string.Empty,
                shipment?.DeliveryAddress is { } home ? Lines(home) : pickup is not null ? new[] { pickup.Name }.Concat(Lines(pickup.Address)).ToArray() : Array.Empty<string>(), shipment?.RecipientPhone);
            string contents = string.Join(", ", contentsOf[row.ShipmentId].Select(parcel => parcel.DeclaredContents)
                .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase));
            waybills.Add(new OperationsHouseWaybill(companyName, agent, dispatch.MasterWaybill(carriers), row.HouseNumber, shipper, consignee, airFrom, airTo, routing, row.Pieces, row.WeightKg, row.ChargeableKg, contents.Length == 0 ? contentsFallback : Cut(contents, 200), row.DeclaredValue,
                row.CurrencyCode, dispatch.ChargesCollect, dispatch.HandlingInstructions, today, origin?.Address.City ?? string.Empty, settings.WeightUnit));
        }

        return waybills;
    }

    public async Task<OperationsShipperLetter> LetterAsync(OperationsDispatch dispatch, string companyName, IReadOnlyDictionary<Guid, OperationsCarrier> carriers, OperationsTenantSettings settings,
        DateOnly today, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(settings);
        IReadOnlyList<OperationsDispatchShipment> rows = await _dispatches.ListDispatchShipmentsAsync(dispatch.Id, cancellationToken);
        OperationsWarehouse? origin = await _locations.GetWarehouseAsync(dispatch.WarehouseId, cancellationToken);
        OperationsWarehouse? destination = dispatch.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        List<OperationsDispatchLeg> legs = dispatch.Legs.OrderBy(leg => leg.Sequence).ToList();
        OperationsCarrier? carrier = legs.FirstOrDefault(leg => leg.Kind == OperationsCarrierKind.Air && leg.CarrierId is not null) is { CarrierId: { } id } ? carriers.GetValueOrDefault(id)
            : legs.FirstOrDefault(leg => leg.CarrierId is not null) is { CarrierId: { } any } ? carriers.GetValueOrDefault(any) : null;
        IReadOnlyList<OperationsBundle> bundles = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);
        (string airFrom, string airTo) = dispatch.AirRoute();
        // Values in different currencies are listed apart, never added (review).
        Dictionary<string, decimal> values = rows.GroupBy(row => row.CurrencyCode.Length > 0 ? row.CurrencyCode : settings.CurrencyCode, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.DeclaredValue), StringComparer.Ordinal);
        return new OperationsShipperLetter(companyName, new OperationsWaybillParty(companyName, origin is null ? Array.Empty<string>() : Lines(origin.Address), origin?.Phone),
            new OperationsWaybillParty(destination?.Name ?? dispatch.DestinationCountryCode, destination is null ? new[] { dispatch.DestinationCountryCode } : Lines(destination.Address), destination?.Phone),
            carrier?.Name, carrier?.AccountNumber, airFrom, airTo, dispatch.MasterWaybill(carriers), dispatch.Number,
            bundles.Count, bundles.Sum(bundle => bundle.WeightKg ?? bundle.ParcelsWeightKg), bundles.Sum(OperationsDispatchCost.ChargeableKg), values, dispatch.ChargesCollect, dispatch.ExportReference, dispatch.Documents,
            dispatch.HandlingInstructions, today, settings.WeightUnit);
    }

    public static string[] Lines(OperationsPostalAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new[] { address.Line1, address.Line2, string.Join(" ", new[] { address.City, address.Region, address.PostalCode }.Where(part => !string.IsNullOrWhiteSpace(part))), address.CountryCode }
            .Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line!).ToArray();
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private async Task<Dictionary<Guid, OperationsShipment>> ShipmentsAsync(IEnumerable<OperationsDispatchShipment> rows, CancellationToken cancellationToken) =>
        (await _dispatches.ListShipmentsAsync(rows.Select(row => row.ShipmentId).Distinct().ToList(), cancellationToken)).ToDictionary(shipment => shipment.Id);
}
