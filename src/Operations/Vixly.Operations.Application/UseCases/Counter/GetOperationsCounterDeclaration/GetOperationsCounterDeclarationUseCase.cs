using System.Globalization;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterDeclaration;

// =========================================================
// FILE: GetOperationsCounterDeclarationUseCase.cs
//
// PURPOSE:
// The page the customer signs at the counter: their declaration of each parcel and the insurance they chose or declined (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve
// - Printed in the customer language; the company keeps the signed paper
// =========================================================
public sealed class GetOperationsCounterDeclarationUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsShipmentBooking _booking;

    private readonly OperationsLiability _liability;

    private readonly IOperationsFileStore _files;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsCounterDeclarationUseCase(
        OperationsCounterDesk desk,
        IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands,
        IOperationsChargeRepository charges,
        OperationsShipmentBooking booking,
        OperationsLiability liability,
        IOperationsFileStore files,
        IOperationsLabelRenderer renderer,
        OperationsNumbering numbering,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _liability = liability ?? throw new ArgumentNullException(nameof(liability));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsCounterDeclarationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCounterDeclarationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCounterDeclarationResponse.Failure(refused);
        }

        // Only a shipment leaving from one of the clerk's counters (as every counter action).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { HasInstructions: true } shipment
            || !counters.Any(counter => counter.Id == shipment.OriginWarehouseId)
            || await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsCounterDeclarationResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // The page carries the shipment's number and barcode: a shipment without one gets it now (as its other documents); two at once: the second reads it back.
        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));

            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                _unitOfWork.DiscardChanges();

                if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { Number: not null } numbered)
                {
                    return GetOperationsCounterDeclarationResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
                }

                shipment = numbered;
            }
        }

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

        // In the customer's language: they read and sign it.
        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(customer.PreferredLanguage.Value);
            return GetOperationsCounterDeclarationResponse.Success(_renderer.RenderCustomerDeclaration(declaration), "declaration-" + (shipment.Number ?? shipment.Id.ToString("N")) + ".pdf");
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
