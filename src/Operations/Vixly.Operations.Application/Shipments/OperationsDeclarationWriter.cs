using System.Globalization;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsDeclarationWriter.cs
//
// PURPOSE:
// What the customer declares and signs for a shipment at the counter (Slice 5D, D-8): its parcels and values, the insurance or the company's liability.
//
// RESPONSIBILITIES:
// - Put together the declaration the printed page and the customer screen show (Slice 5F step 3, P-3)
//
// IMPORTANT NOTES:
// - The same words on paper and on screen; the caller checks who may see it
// =========================================================
public sealed class OperationsDeclarationWriter
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsShipmentBooking _booking;

    private readonly OperationsLiability _liability;

    private readonly IOperationsFileStore _files;

    private readonly TimeProvider _timeProvider;

    public OperationsDeclarationWriter(IOperationsReceivingRepository receiving, IOperationsLocationRepository locations, IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands, IOperationsChargeRepository charges, OperationsShipmentBooking booking, OperationsLiability liability, IOperationsFileStore files,
        TimeProvider timeProvider)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _liability = liability ?? throw new ArgumentNullException(nameof(liability));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OperationsCustomerDeclaration> BuildAsync(OperationsShipment shipment, OperationsCustomer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(customer);

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsWeightUnit unit = settings?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        string weightUnit = unit == OperationsWeightUnit.Pound ? "lb" : "kg";
        IReadOnlyList<OperationsParcel> parcels = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken));
        Dictionary<Guid, OperationsWarehouse> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(warehouse => warehouse.Id);
        string route = (warehouses.TryGetValue(shipment.OriginWarehouseId, out OperationsWarehouse? origin) ? origin.Name : string.Empty) + " → "
            + (shipment.DestinationWarehouseId is { } to && warehouses.TryGetValue(to, out OperationsWarehouse? destination) ? destination.Name : shipment.DestinationCountryCode ?? string.Empty);
        string Weight(decimal? kilograms) => kilograms is { } value ? OperationsUnits.ToShownWeight(value, unit).ToString("0.##", CultureInfo.InvariantCulture) + " " + weightUnit : "—";

        // The insurance chosen with its premium for these parcels and its terms; none: the company's liability words for the mode.
        OperationsTransportMode mode = OperationsTransportModes.Of(shipment.ServiceCode);
        IReadOnlyList<OperationsInsurancePlan> products = (await _charges.ListInsurancePlansAsync(cancellationToken)).Where(plan => plan.Mode == mode).ToList();
        OperationsInsurancePlan? product = !shipment.Options.HasFlag(OperationsShipmentOptions.Insured) ? null
            : shipment.InsurancePlanId is { } planId ? products.FirstOrDefault(plan => plan.Id == planId) : products.OrderBy(plan => plan.Position).FirstOrDefault();
        decimal? premium = null;

        if (product is not null)
        {
            (decimal? _, IReadOnlyDictionary<Guid, decimal?> premiums) = await _booking.InsuranceOptionsAsync(shipment, parcels, new[] { product }, cancellationToken);
            premium = premiums.TryGetValue(product.Id, out decimal? found) ? found : null;
        }

        string? liabilityText = product is null ? (await _liability.ListAsync(cancellationToken)).FirstOrDefault(view => view.Mode == mode)?.CustomerText : null;
        byte[]? signature = shipment.CustomerSignatureKey is { } key ? await _files.ReadAsync(shipment.TenantId, key, cancellationToken) : null;

        OperationsCustomerDeclaration declaration = new(
            (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty,
            shipment.Number ?? string.Empty,
            DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime),
            customer.FirstName + " " + customer.LastName + (customer.Code is { } code ? " · " + code : string.Empty),
            (shipment.RecipientName ?? string.Empty) + (shipment.RecipientPhone is { } phone ? " · " + phone : string.Empty),
            route,
            shipment.ServiceCode,
            parcels.Select(parcel => new OperationsDeclarationLine(parcel.Number ?? "—", parcel.DeclaredContents ?? "—", parcel.DeclaredValue ?? 0m, Weight(parcel.WeightKg))).ToList(),
            parcels.Sum(parcel => parcel.DeclaredValue ?? 0m),
            Weight(parcels.Sum(parcel => parcel.WeightKg ?? 0m)),
            settings?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency,
            product?.Name,
            product is null ? null : parcels.Sum(parcel => parcel.DeclaredValue ?? 0m),
            premium,
            product?.VersionAt(_timeProvider.GetUtcNow())?.Coverage ?? product?.Versions.LastOrDefault()?.Coverage,
            liabilityText,
            signature,
            shipment.CustomerSignedUtc);

        return declaration;
    }
}
