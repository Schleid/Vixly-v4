using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.PriceOperationsShipment;

// =========================================================
// FILE: PriceOperationsShipmentUseCase.cs
//
// PURPOSE:
// Prices a shipment for a customer to a zone, on one service or every offered one, for a quote (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class PriceOperationsShipmentUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsRater _rater;

    private readonly IOperationsCustomerRepository _customers;

    public PriceOperationsShipmentUseCase(
        OperationsAccess access,
        OperationsRater rater,
        IOperationsCustomerRepository customers)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _rater = rater ?? throw new ArgumentNullException(nameof(rater));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<PriceOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PriceOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.QuotesCreate, false, cancellationToken);

        if (refused is not null)
        {
            return PriceOperationsShipmentResponse.Failure(refused);
        }

        OperationsCustomer? customer = null;

        if (!string.IsNullOrWhiteSpace(request.CustomerCode)
            && (customer = await _customers.FindByCodeAsync(request.CustomerCode.Trim().ToUpperInvariant(), cancellationToken)) is null)
        {
            return PriceOperationsShipmentResponse.Failure(OperationsChargeErrors.CustomerNotFound);
        }

        OperationsRatingSetup setup = await _rater.LoadAsync(context.RequiredTenantId, customer?.Id, cancellationToken);
        DateTimeOffset utcNow = _rater.UtcNow;
        List<OperationsServiceOption> services = PlatformShippingServices.All.Where(service => setup.Offers(service.Code))
            .Select(service => new OperationsServiceOption(service.Code, setup.Label(service.Code))).ToList();
        List<OperationsChargeExtra> adjustable = setup.Extras.Where(extra => OperationsRatingEngine.Adjustable(extra, utcNow)).ToList();
        List<OperationsItemType> types = setup.ItemTypes.Where(type => type.IsActive).ToList();
        IReadOnlyList<OperationsRating> ratings = Array.Empty<OperationsRating>();

        // Opened without pieces: the empty form.
        if (request.Pieces is { Count: > 0 })
        {
            if (setup.Pieces(request.Pieces) is not { } pieces)
            {
                return PriceOperationsShipmentResponse.Failure(OperationsChargeErrors.PiecesInvalid);
            }

            if (!setup.TryRoute(request.OriginWarehouseId, request.DestinationWarehouseId, out OperationsWarehouse? from, out OperationsWarehouse to))
            {
                return PriceOperationsShipmentResponse.Failure(OperationsChargeErrors.RouteInvalid);
            }

            if (request.Delivery != OperationsDeliveryModes.None && !to.Offers(request.Delivery))
            {
                return PriceOperationsShipmentResponse.Failure(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.DeliveryModeUnavailable);
            }

            ratings = string.IsNullOrWhiteSpace(request.ServiceCode)
                ? setup.RateOffered(from, to, customer?.Id, pieces, request.Consolidated, request.Insured, utcNow, request.Delivery, request.ExtraQuantities)
                : setup.Rate(request.ServiceCode.Trim(), from, to, customer?.Id, pieces, request.Consolidated, request.Insured, utcNow, request.Delivery, request.ExtraQuantities) is { } rating
                    ? new[] { rating }
                    : Array.Empty<OperationsRating>();

            if (ratings.Count == 0)
            {
                return PriceOperationsShipmentResponse.Failure(OperationsChargeErrors.NotPriced);
            }
        }

        return PriceOperationsShipmentResponse.Success(ratings, customer?.Id, customer?.FullName, adjustable, services, setup.Origins, setup.Destinations, types, setup.Settings.WeightUnit, setup.Settings.DimensionUnit,
            setup.Settings.CurrencyCode);
    }
}
