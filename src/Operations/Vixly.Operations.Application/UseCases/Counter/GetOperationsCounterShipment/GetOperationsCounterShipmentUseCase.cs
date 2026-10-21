using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterShipment;

// =========================================================
// FILE: GetOperationsCounterShipmentUseCase.cs
//
// PURPOSE:
// Shows a shipment at the counter: its parcels against the count, and the amount so far or, once final, its amount (Slice 5C, C-7 and C-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve; parcels are added only where the clerk serves (Shipment.CanServe)
// =========================================================
public sealed class GetOperationsCounterShipmentUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsShipmentBooking _booking;

    private readonly OperationsShipmentIntake _intake;

    private readonly Vixly.Operations.Application.Charges.OperationsShipmentEstimator _estimator;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsBillingRepository _billing;

    public GetOperationsCounterShipmentUseCase(
        OperationsCounterDesk desk,
        IOperationsCustomerRepository customers,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        OperationsShipmentBooking booking,
        OperationsShipmentIntake intake,
        Vixly.Operations.Application.Charges.OperationsShipmentEstimator estimator,
        IOperationsSupplyRepository supplies,
        IOperationsBillingRepository billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
    }

    public async Task<GetOperationsCounterShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCounterShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCounterShipmentResponse.Failure(refused);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment
            || await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsCounterShipmentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        IReadOnlyList<OperationsParcel> parcels = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken));
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        // Final: its amount as fixed then; otherwise the price of what is in so far (C-8).
        (Vixly.Operations.Application.Charges.OperationsRating? estimate, string currency) = shipment.IsBooked
            ? (null, shipment.BookedCurrencyCode ?? string.Empty)
            : await _booking.PriceAsync(shipment, parcels, cancellationToken);

        // Slice 5D: the products it may have, and what each would cost for the parcels in so far.
        IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> products = OperationsShipmentIntake.ProductsFor(await _intake.InsuranceAsync(cancellationToken), shipment.ServiceCode);
        (decimal? baseTotal, IReadOnlyDictionary<Guid, decimal?> premiums) = shipment.IsBooked
            ? (null, new Dictionary<Guid, decimal?>())
            : await _booking.InsuranceOptionsAsync(shipment, parcels, products, cancellationToken);

        // Slice 5E: the supplies added to it at the counter.
        Vixly.Operations.Domain.Supplies.OperationsSupplySale? sale = (await _supplies.ListShipmentSalesAsync(new[] { shipment.Id }, cancellationToken)).FirstOrDefault();

        return GetOperationsCounterShipmentResponse.Success(sale, OperationsCounterCustomer.Of(customer), await _desk.RowAsync(shipment, counters, cancellationToken), parcels, estimate, currency,
            settings?.WeightUnit ?? OperationsWeightUnit.Kilogram, settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre, await _desk.StandingAsync(customer, cancellationToken),
            // Slice 5E: the insurance choices' totals count the supplies too, as the estimate does (review of 5E).
            products, baseTotal is null ? null : premiums, baseTotal + (sale?.Total ?? 0m), settings?.StaffMayChangeInsurance ?? true, _intake.UtcNow,
            await _estimator.EstimateAsync(shipment.TenantId, new Vixly.Operations.Application.Charges.OperationsEstimateInput(shipment.CustomerId, shipment.ServiceCode,
                shipment.OriginWarehouseId, shipment.DestinationWarehouseId, shipment.DeliveryAddress is null ? OperationsDeliveryModes.Pickup : OperationsDeliveryModes.HomeDelivery,
                shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate), shipment.Options.HasFlag(OperationsShipmentOptions.Insured)
                    ? shipment.InsurancePlanId ?? products.FirstOrDefault()?.Id : null,
                parcels.Where(parcel => parcel.WeightKg is > 0m).Select(parcel => ((string?)parcel.Number, new Vixly.Operations.Application.Charges.OperationsRatingPiece(parcel.WeightKg!.Value,
                    parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m, parcel.DeclaredValue ?? 0m, null, parcel.DeclaredContents))).ToList(), null,
                Vixly.Operations.Application.Charges.OperationsEstimateSupply.Of(sale, sale is null ? Array.Empty<Vixly.Operations.Domain.Supplies.OperationsSupply>()
                    : await _supplies.ListAsync(cancellationToken), System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)), products, cancellationToken))
            .Invoiced(shipment.IsBooked ? await _billing.FindShipmentInvoiceAsync(shipment.Id, cancellationToken) : null);
    }
}
