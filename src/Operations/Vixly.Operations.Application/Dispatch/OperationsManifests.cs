using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsManifests.cs
//
// PURPOSE:
// Builds the bundles of a manifest with their shipments (Slice 6B, D-6).
//
// RESPONSIBILITIES:
// - Group a bundle's parcels by shipment with the customer's code, the consignee, pieces, weight, value and contents
//
// IMPORTANT NOTES:
// - Parcels without a shipment are listed under their own number
// =========================================================
public sealed class OperationsManifests
{
    private readonly IOperationsDispatchRepository _dispatches;
    private readonly IOperationsCustomerRepository _customers;

    public OperationsManifests(IOperationsDispatchRepository dispatches, IOperationsCustomerRepository customers)
    {
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<OperationsManifestBundle> BundleAsync(OperationsBundle bundle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        return (await BundlesAsync(new[] { bundle }, await _dispatches.ListBundleParcelsAsync(bundle.Id, cancellationToken), cancellationToken)).Single();
    }

    // The dispatch's bundles with their parcels read in one query, their shipments and customers in one each (review).
    public async Task<IReadOnlyList<OperationsManifestBundle>> DispatchAsync(Guid dispatchId, IReadOnlyList<OperationsBundle> bundles, CancellationToken cancellationToken) =>
        await BundlesAsync(bundles, await _dispatches.ListDispatchParcelsAsync(dispatchId, false, cancellationToken), cancellationToken);

    private async Task<IReadOnlyList<OperationsManifestBundle>> BundlesAsync(IReadOnlyList<OperationsBundle> bundles, IReadOnlyList<OperationsParcel> all, CancellationToken cancellationToken)
    {
        Dictionary<Guid, OperationsShipment> shipments = (await _dispatches.ListShipmentsAsync(all.Where(parcel => parcel.ShipmentId is not null).Select(parcel => parcel.ShipmentId!.Value).Distinct().ToList(),
            cancellationToken)).ToDictionary(shipment => shipment.Id);
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(shipments.Values.Select(shipment => shipment.CustomerId).Distinct().ToList(), cancellationToken))
            .ToDictionary(customer => customer.Id);
        ILookup<Guid?, OperationsParcel> byBundle = all.ToLookup(parcel => parcel.BundleId);
        return bundles.Select(bundle => Bundle(bundle, byBundle[bundle.Id].ToList(), shipments, customers)).ToList();
    }

    private static OperationsManifestBundle Bundle(OperationsBundle bundle, IReadOnlyList<OperationsParcel> parcels, IReadOnlyDictionary<Guid, OperationsShipment> shipments,
        IReadOnlyDictionary<Guid, OperationsCustomer> customers)
    {
        List<OperationsManifestShipment> lines = parcels
            .GroupBy(parcel => parcel.ShipmentId is { } id && shipments.ContainsKey(id) ? id : parcel.Id)
            .Select(group =>
            {
                OperationsShipment? shipment = shipments.GetValueOrDefault(group.Key);
                OperationsCustomer? customer = shipment is null ? null : customers.GetValueOrDefault(shipment.CustomerId);
                return new OperationsManifestShipment(shipment?.Number ?? group.First().Number ?? string.Empty, customer?.Code ?? string.Empty, shipment?.RecipientName ?? customer?.FullName ?? string.Empty,
                    shipment?.DestinationCountryCode ?? bundle.DestinationCountryCode, group.Count(), group.Sum(parcel => parcel.WeightKg ?? 0m), group.Sum(parcel => parcel.DeclaredValue ?? 0m),
                    group.Select(parcel => parcel.CurrencyCode).FirstOrDefault(code => code is not null) ?? string.Empty,
                    string.Join(", ", group.Select(parcel => parcel.DeclaredContents).Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase)));
            })
            .OrderBy(line => line.Number, StringComparer.Ordinal).ToList();
        return new OperationsManifestBundle(bundle.Number, bundle.TypeName, parcels.Count, bundle.WeightKg, bundle.LengthCm, bundle.WidthCm, bundle.HeightCm, lines);
    }
}
