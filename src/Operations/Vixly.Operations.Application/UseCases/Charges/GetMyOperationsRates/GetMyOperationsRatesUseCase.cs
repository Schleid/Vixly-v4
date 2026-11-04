using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetMyOperationsRates;

// =========================================================
// FILE: GetMyOperationsRatesUseCase.cs
//
// PURPOSE:
// The customer's rate calculator to a zone and the estimate of their shipment, always an estimate (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetMyOperationsRatesUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsRater _rater;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsChargeRepository _charges;

    public GetMyOperationsRatesUseCase(
        OperationsCustomerAccounts accounts,
        OperationsRater rater,
        IOperationsReceivingRepository receiving,
        IOperationsChargeRepository charges)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _rater = rater ?? throw new ArgumentNullException(nameof(rater));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
    }

    public async Task<GetMyOperationsRatesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsRatesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsRatesResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        Guid customerId = signedIn.Customer.Id;
        OperationsRatingSetup setup = await _rater.LoadAsync(context.RequiredTenantId, customerId, cancellationToken);
        List<OperationsServiceOption> services = PlatformShippingServices.All.Where(service => setup.Offers(service.Code))
            .Select(service => new OperationsServiceOption(service.Code, setup.Label(service.Code))).ToList();
        DateTimeOffset utcNow = _rater.UtcNow;
        List<OperationsQuote> quotes = (await _charges.ListQuotesAsync(customerId, 20, cancellationToken)).Where(quote => quote.IsValid(utcNow)).ToList();
        IReadOnlyList<OperationsRating> ratings = Array.Empty<OperationsRating>();
        int unmeasured = 0;
        OperationsWarehouse? shipmentDestination = null;

        if (request.ShipmentId is { } shipmentId)
        {
            // Only the customer's own shipment; another's is "not found" (S-3).
            if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != customerId)
            {
                return GetMyOperationsRatesResponse.Failure(OperationsShipmentErrors.NotFound);
            }

            IReadOnlyList<OperationsParcel> parcels = await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken);
            List<OperationsRatingPiece> pieces = parcels.Where(parcel => parcel.WeightKg is > 0)
                .Select(parcel => new OperationsRatingPiece(parcel.WeightKg!.Value, parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m, parcel.DeclaredValue ?? 0m, null,
                    parcel.DeclaredContents)).ToList();
            unmeasured = parcels.Count - pieces.Count;
            shipmentDestination = setup.Destination(shipment.DestinationWarehouseId);

            // Slice 4C: the shipment's route, from the warehouse its packages were dropped off at to the destination the customer chose.
            if (pieces.Count > 0 && shipmentDestination is not null)
            {
                OperationsWarehouse? origin = setup.Warehouses.FirstOrDefault(warehouse => warehouse.Id == shipment.OriginWarehouseId);
                OperationsDeliveryModes delivery = shipment.DestinationBranchId is not null ? OperationsDeliveryModes.Pickup : OperationsDeliveryModes.HomeDelivery;
                ratings = setup.RateOffered(origin, shipmentDestination, customerId, pieces, shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate),
                    shipment.Options.HasFlag(OperationsShipmentOptions.Insured), utcNow, delivery);
            }
        }
        else if (request.Pieces is { Count: > 0 })
        {
            if (setup.Pieces(request.Pieces) is not { } pieces)
            {
                return GetMyOperationsRatesResponse.Failure(OperationsChargeErrors.PiecesInvalid);
            }

            // Slice 4C: the calculator offers the company's own routes: from its origins (the only one when there is one) to its destinations.
            if (!setup.TryRoute(request.OriginWarehouseId, request.DestinationWarehouseId, out OperationsWarehouse? from, out OperationsWarehouse to))
            {
                return GetMyOperationsRatesResponse.Failure(OperationsChargeErrors.RouteInvalid);
            }

            // Pickup where the destination offers it, unless the customer asks for home delivery; extras limited to a delivery mode then count.
            if (request.HomeDelivery && !to.Offers(OperationsDeliveryModes.HomeDelivery))
            {
                return GetMyOperationsRatesResponse.Failure(OperationsShipmentErrors.DeliveryModeUnavailable);
            }

            OperationsDeliveryModes delivery = request.HomeDelivery || !to.Offers(OperationsDeliveryModes.Pickup) ? OperationsDeliveryModes.HomeDelivery : OperationsDeliveryModes.Pickup;
            ratings = setup.RateOffered(from, to, customerId, pieces, request.Consolidated, request.Insured, utcNow, delivery);

            if (ratings.Count == 0)
            {
                return GetMyOperationsRatesResponse.Failure(OperationsChargeErrors.NotPriced);
            }
        }

        return GetMyOperationsRatesResponse.Success(ratings, services, setup.Origins, setup.Destinations, setup.Settings.WeightUnit, setup.Settings.DimensionUnit,
            setup.Settings.CurrencyCode, quotes, unmeasured, shipmentDestination);
    }
}
