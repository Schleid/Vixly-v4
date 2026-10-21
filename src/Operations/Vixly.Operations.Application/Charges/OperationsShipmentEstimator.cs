using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsShipmentEstimator.cs
//
// PURPOSE:
// Computes the shipment estimate the clerk and the customer see beside the shipment (Slice 5D: "the customer says add insurance, what is the cost now?").
//
// RESPONSIBILITIES:
// - Price the shipment as it stands with the company's prices: its own charges, then each parcel, then the total
// - While no parcel is in: the service's charges and rates, and the insurance on the expected value
// - The "what if" choices: each insurance product the service may have, no insurance, consolidation on or off
//
// IMPORTANT NOTES:
// - The same rating as the calculator, quotes, the counter and booking: one set of numbers everywhere
// - Read only: nothing is saved
// =========================================================
public sealed class OperationsShipmentEstimator
{
    private readonly OperationsRater _rater;

    public OperationsShipmentEstimator(OperationsRater rater)
    {
        _rater = rater ?? throw new ArgumentNullException(nameof(rater));
    }

    public async Task<OperationsEstimate> EstimateAsync(Guid tenantId, OperationsEstimateInput input, IReadOnlyList<OperationsInsurancePlan> products,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(products);
        OperationsRatingSetup setup = await _rater.LoadAsync(tenantId, input.CustomerId, cancellationToken);
        string currency = setup.Settings.CurrencyCode;

        if (input.ServiceCode is not { Length: > 0 } service)
        {
            return OperationsEstimate.NotPriced(currency);
        }

        DateTimeOffset at = _rater.UtcNow;
        OperationsWarehouse? origin = setup.Warehouses.FirstOrDefault(warehouse => warehouse.Id == input.OriginWarehouseId);
        OperationsWarehouse? destination = setup.Destination(input.DestinationWarehouseId);
        IReadOnlyList<OperationsRatingPiece> pieces = input.Parcels.Select(parcel => parcel.Piece).ToList();
        decimal? value = pieces.Count > 0 ? pieces.Sum(piece => piece.DeclaredValue) : input.ExpectedValue;

        // The premium on the value the customer expects while no parcel is in, in cents; null when that value cannot be insured (above the maximum).
        decimal? Expected(Guid planId) => input.ExpectedValue is { } expected
            && setup.InsuranceFor(OperationsTransportModes.Of(service), planId)?.VersionAt(at)?.Premium(expected) is { } premium
            ? Math.Round(premium, 2, MidpointRounding.AwayFromZero)
            : null;

        (decimal Total, OperationsRating? Rating, bool Possible) Price(bool consolidate, Guid? planId)
        {
            OperationsRating? rating = setup.Rate(service, origin, destination, input.CustomerId, pieces, consolidate, planId is not null, at, input.Delivery, null, planId);

            if (rating is null)
            {
                return (0m, null, false);
            }

            // No parcel yet: the insurance on the value the customer expects, else its minimum.
            decimal total = rating.Total;

            if (planId is { } id && pieces.Count == 0 && input.ExpectedValue is not null)
            {
                if (Expected(id) is not { } premium)
                {
                    return (total - rating.Lines.Where(line => line.Kind == OperationsChargeLineKind.Insurance).Sum(line => line.Amount), rating, false);
                }

                total += premium - rating.Lines.Where(line => line.Kind == OperationsChargeLineKind.Insurance).Sum(line => line.Amount);
            }

            return (total, rating, !rating.InsuranceRefused);
        }

        // Slice 5E: the supplies on the shipment are added to every total (they do not change with the choices).
        IReadOnlyList<OperationsEstimateSupply> supplies = input.Supplies ?? Array.Empty<OperationsEstimateSupply>();
        decimal suppliesTotal = supplies.Sum(line => line.Amount);
        (decimal total, OperationsRating? current, _) = Price(input.Consolidate, input.InsurancePlanId);

        if (current is null)
        {
            return OperationsEstimate.NotPriced(currency);
        }

        // Each parcel's own lines (freight, item price), labelled as the clerk knows it.
        List<OperationsEstimateParcel> parcels = new();

        for (int index = 0; index < input.Parcels.Count; index++)
        {
            OperationsRatingPiece piece = input.Parcels[index].Piece;
            List<OperationsChargeLine> own = current.Lines.Where(line => line.Piece == index + 1).ToList();
            parcels.Add(new OperationsEstimateParcel(index + 1, input.Parcels[index].Label, piece.WeightKg, piece.LengthCm, piece.WidthCm, piece.HeightCm,
                own.Select(line => line.Basis).FirstOrDefault(basis => basis is not null), own.Sum(line => line.Amount)));
        }

        List<OperationsChargeLine> shipment = current.Lines.Where(line => line.Piece is null).ToList();

        // The insurance line on the expected value while no parcel is in.
        // Above the product's maximum: no insurance line, and the what if says the product is not possible.
        if (pieces.Count == 0 && input.InsurancePlanId is { } plan && input.ExpectedValue is { } expectedValue)
        {
            shipment = shipment.Where(line => line.Kind != OperationsChargeLineKind.Insurance).ToList();

            if (Expected(plan) is { } expectedPremium)
            {
                shipment.Add(new OperationsChargeLine(OperationsChargeLineKind.Insurance, null, null, 1, expectedValue, expectedPremium, expectedPremium));
            }
        }

        // What if: each product the service may have, no insurance, consolidation the other way when it costs something.
        List<OperationsEstimateOption> whatIf = new();
        decimal none = Price(input.Consolidate, null).Total;
        whatIf.Add(new OperationsEstimateOption("none", null, null, none - total, none, input.InsurancePlanId is null));

        foreach (OperationsInsurancePlan product in products.Where(product => product.Mode == OperationsTransportModes.Of(service) && product.IsActive))
        {
            (decimal withProduct, _, bool possible) = Price(input.Consolidate, product.Id);
            whatIf.Add(new OperationsEstimateOption("product", product.Id, product.Name, possible ? withProduct - total : null, possible ? withProduct : null,
                input.InsurancePlanId == product.Id));
        }

        OperationsRateCard? card = OperationsRateCardChoice.Pick(setup.Cards, service, origin?.Id, destination?.Id, input.CustomerId, at);
        OperationsPriceVersion? rates = card?.VersionAt(at);

        if (rates?.ConsolidationFee is > 0)
        {
            decimal other = Price(!input.Consolidate, input.InsurancePlanId).Total;
            whatIf.Add(new OperationsEstimateOption(input.Consolidate ? "separate" : "consolidate", null, null, other - total, other, false));
        }

        return new OperationsEstimate(true, currency, shipment, parcels, total + suppliesTotal, value, pieces.Count == 0 ? rates : null, card?.Basis ?? OperationsRateBasis.Weight,
            whatIf.Select(option => option.Total is { } optionTotal ? option with { Total = optionTotal + suppliesTotal } : option).ToList(), supplies);
    }
}
