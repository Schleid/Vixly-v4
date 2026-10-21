using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Operations.Rates;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.UseCases.Charges.GetMyOperationsRates;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalRateEndpoints.cs
//
// PURPOSE:
// The customer portal's rate calculator and shipment estimate (Slice 4 design section 8).
//
// RESPONSIBILITIES:
// - POST /internal/v1/customers/me/rates: the estimates for typed pieces, or for one of the customer's shipments, and the customer's quotes
//
// IMPORTANT NOTES:
// - Customer scope only; the session header names the customer (S-3)
// - Never another customer's prices
// =========================================================
public static class OperationsInternalRateEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalRateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder rates = endpoints.MapGroup("/internal/v1/customers/me")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        rates.MapPost("/rates", async (HttpContext http, CustomerRatesBody body, GetMyOperationsRatesUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsRatesResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new GetMyOperationsRatesRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http),
                OriginWarehouseId = body.Origin,
                DestinationWarehouseId = body.Destination,
                HomeDelivery = body.HomeDelivery,
                Pieces = body.Pieces?.Take(OperationsChargeLimits.MaxPieces + 1).Select(piece => new OperationsPieceInput(piece.Weight, piece.Length, piece.Width, piece.Height, piece.Value)).ToList(),
                Consolidated = body.Consolidated,
                Insured = body.Insured,
                ShipmentId = body.ShipmentId
            }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new CustomerRatesDto(
                    response.Destinations.Select(destination => destination.Address.CountryCode).Distinct(StringComparer.Ordinal).ToList(),
                    OperationsInternalPackageEndpoints.Weight(response.WeightUnit),
                    OperationsInternalPackageEndpoints.Length(response.DimensionUnit),
                    response.CurrencyCode,
                    response.Ratings.Select(rating => new CustomerRateEstimateDto(rating.ServiceCode, response.Services.FirstOrDefault(service => service.Code == rating.ServiceCode)?.Label,
                        rating.Lines.Select(Line).ToList(), rating.Total, rating.InsuranceRefused)).ToList(),
                    response.Quotes.Select(quote => new CustomerQuoteDto(quote.Number, quote.ServiceCode, quote.CountryCode, quote.Total, quote.StandardTotal, quote.ValidUntilUtc,
                        quote.Lines.Select(line => new CustomerChargeLineDto(line.Kind.ToString(), line.Label, line.Piece, line.Quantity, line.Amount)).ToList(), quote.CurrencyCode)).ToList(),
                    response.Unmeasured,
                    response.ShipmentDestination?.Address.CountryCode,
                    response.Services.Select(service => new CustomerServiceDto(service.Code, service.Label)).ToList(),
                    response.Destinations.Select(OperationsCustomerRouteDtos.Destination).ToList(),
                    response.ShipmentDestination?.Name,
                    response.Origins.Select(origin => new Vixly.Contracts.Operations.Packages.CustomerChoiceDto(origin.Id, origin.Name, origin.Address.City + ", " + origin.Address.CountryCode))
                        .ToList()))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 5D: the shipment estimate while the customer builds or changes a shipment, and on their shipment. Read only.
        rates.MapPost("/shipments/estimate", async (HttpContext http, CustomerShipmentEstimateBody body,
            Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment.EstimateMyOperationsShipmentUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment.EstimateMyOperationsShipmentResponse response = await useCase.ExecuteAsync(
                OperationsInternalPackageEndpoints.Context(http), new Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment.EstimateMyOperationsShipmentRequest
                {
                    SessionToken = OperationsInternalPackageEndpoints.Session(http),
                    Draft = body.Instructions is null ? null : OperationsInternalShipmentEndpoints.Draft(body.Instructions),
                    OriginWarehouseId = body.OriginId,
                    ExpectedValue = body.ExpectedValue,
                    ShipmentId = body.ShipmentId
                }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            Vixly.Operations.Domain.Settings.OperationsWeightUnit weight = response.WeightUnit;
            Vixly.Operations.Domain.Settings.OperationsDimensionUnit length = response.DimensionUnit;
            OperationsEstimate? estimate = response.Estimate;
            return Results.Ok(estimate is not { Priced: true }
                ? new CustomerShipmentEstimateDto(false, estimate?.CurrencyCode ?? string.Empty, OperationsInternalPackageEndpoints.Weight(weight), OperationsInternalPackageEndpoints.Length(length),
                    "Weight", Array.Empty<CustomerChargeLineDto>(), Array.Empty<CustomerEstimateParcelDto>(), 0m, null, Array.Empty<CustomerRateBandDto>(), Array.Empty<CustomerEstimateOptionDto>())
                : new CustomerShipmentEstimateDto(true, estimate.CurrencyCode, OperationsInternalPackageEndpoints.Weight(weight), OperationsInternalPackageEndpoints.Length(length),
                    estimate.Basis.ToString(), estimate.Shipment.Select(Line).ToList(),
                    estimate.Parcels.Select(parcel => new CustomerEstimateParcelDto(parcel.Number, parcel.Label, Vixly.Operations.Application.Settings.OperationsUnits.ToShownWeight(parcel.WeightKg, weight),
                        Vixly.Operations.Application.Settings.OperationsUnits.ToShownLength(parcel.LengthCm, length), Vixly.Operations.Application.Settings.OperationsUnits.ToShownLength(parcel.WidthCm, length),
                        Vixly.Operations.Application.Settings.OperationsUnits.ToShownLength(parcel.HeightCm, length), parcel.Billable, parcel.Amount)).ToList(),
                    estimate.Total, estimate.Rates?.MinimumCharge,
                    (estimate.Rates?.Bands ?? Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateBand>()).Select(band => new CustomerRateBandDto(band.Kind.ToString(), band.From, band.Amount, band.Base)).ToList(),
                    estimate.WhatIf.Select(option => new CustomerEstimateOptionDto(option.Kind, option.PlanId, option.Name, option.Change, option.Total, option.Current)).ToList(),
                    estimate.SupplyLines.Select(line => new CustomerEstimateSupplyDto(line.Code, line.Name, line.Quantity, line.UnitPrice, line.DiscountPercent, line.Amount)).ToList()));
        });

        return endpoints;
    }

    private static CustomerChargeLineDto Line(OperationsChargeLine line) => new(line.Kind.ToString(), line.Label, line.Piece, line.Quantity, line.Amount);
}
