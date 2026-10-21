using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRatingEngine.cs
//
// PURPOSE:
// The one rating engine behind quotes, the calculator, estimates and later invoices (Slice 4; bands, versions and zones, Slice 4B).
//
// RESPONSIBILITIES:
// - Price each piece: its item price, else its band (flat, per unit, or base plus per unit) by billable weight or size
// - Add the minimum, the service and consolidation fees, the extras and the insurance, each in the version in force at the pricing date
//
// IMPORTANT NOTES:
// - Pure: no database, so the same input always gives the same lines
// - Billable weight: the larger of weight and volumetric weight, rounded up to the next whole lb or kg (owner); size up to 0.01 ft³ or m³
// - Each line is rounded half away from zero to two decimals; the total is their sum
// =========================================================
public static class OperationsRatingEngine
{
    // Null when the price list has no version in force at the pricing date.
    public static OperationsRating? Rate(OperationsRatingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        OperationsRateCard card = input.Card;

        if (card.VersionAt(input.At) is not { } version)
        {
            return null;
        }

        List<OperationsChargeLine> lines = new();

        for (int index = 0; index < input.Pieces.Count; index++)
        {
            // Ground with only a price per shipment: no line per package.
            if (Freight(input, card, version, input.Pieces[index], index + 1) is { } line)
            {
                lines.Add(line);
            }
        }

        // Slice 4C, ground: a flat price for the whole shipment, on its own line.
        if (version.PerShipment is > 0 and { } perShipment && input.Pieces.Count > 0)
        {
            lines.Add(new OperationsChargeLine(OperationsChargeLineKind.ShipmentFreight, null, null, 1, null, perShipment, Money(perShipment)));
        }

        decimal freight = lines.Sum(line => line.Amount);

        // The minimum is for the shipment on this service (A-3), as its own line.
        if (version.MinimumCharge is { } minimum && freight < minimum && input.Pieces.Count > 0)
        {
            lines.Add(new OperationsChargeLine(OperationsChargeLineKind.Minimum, null, null, 1, null, minimum - freight, Money(minimum - freight)));
            freight = minimum;
        }

        if (version.ServiceFee is > 0 and { } serviceFee)
        {
            lines.Add(new OperationsChargeLine(OperationsChargeLineKind.ServiceFee, null, null, 1, null, serviceFee, Money(serviceFee)));
        }

        if (input.Consolidated && version.ConsolidationFee is > 0 and { } consolidation)
        {
            lines.Add(new OperationsChargeLine(OperationsChargeLineKind.Consolidation, null, null, 1, null, consolidation, Money(consolidation)));
        }

        foreach (OperationsChargeExtra extra in input.Extras.Where(extra => extra.AppliesTo(card.ServiceCode, input.CountryCode, input.OriginWarehouseId, input.DestinationWarehouseId, input.Delivery))
            .OrderBy(extra => extra.Name, StringComparer.Ordinal))
        {
            if (extra.VersionAt(input.At) is { } terms && Extra(input, extra, terms, freight) is { } line)
            {
                lines.Add(line);
            }
        }

        bool refused = false;

        if (input.Insured)
        {
            decimal value = input.Pieces.Sum(piece => piece.DeclaredValue);

            // Asked for insurance without a plan in force, or for a value the plan does not cover: it cannot be insured, and says so.
            if (input.Insurance?.VersionAt(input.At)?.Premium(value, input.Pieces.Select(piece => piece.DeclaredValue)) is { } premium)
            {
                lines.Add(new OperationsChargeLine(OperationsChargeLineKind.Insurance, null, null, 1, value, premium, Money(premium)));
            }
            else
            {
                refused = true;
            }
        }

        return new OperationsRating(card.ServiceCode, card.Id, card.CustomerId is not null, lines, lines.Sum(line => line.Amount), refused, version.Id);
    }

    // The billable weight of a piece in the company's unit, rounded up to a whole unit.
    public static decimal BillableWeight(OperationsRatingPiece piece, decimal? divisorCm3PerKg, OperationsWeightUnit unit)
    {
        ArgumentNullException.ThrowIfNull(piece);
        decimal kilograms = piece.WeightKg;

        if (divisorCm3PerKg is > 0 and { } divisor)
        {
            kilograms = Math.Max(kilograms, piece.LengthCm * piece.WidthCm * piece.HeightCm / divisor);
        }

        // Two decimals first: 10 lb entered and stored in kg is 10 again, never 10.0000001 rounded up to 11.
        return Math.Max(1m, decimal.Ceiling(decimal.Round(OperationsUnits.ToShownWeightExact(kilograms, unit), 2, MidpointRounding.AwayFromZero)));
    }

    public static decimal BillableVolume(OperationsRatingPiece piece, OperationsDimensionUnit unit)
    {
        ArgumentNullException.ThrowIfNull(piece);
        decimal volume = decimal.Round(OperationsUnits.ToShownVolume(piece.LengthCm * piece.WidthCm * piece.HeightCm, unit), 4, MidpointRounding.AwayFromZero);
        return Math.Max(0.01m, decimal.Ceiling(volume * 100m) / 100m);
    }

    // The extras whose quantity staff may give on a quote.
    public static bool Adjustable(OperationsChargeExtra extra, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(extra);
        return extra.Trigger == OperationsExtraTrigger.Manual
            || (extra.Trigger == OperationsExtraTrigger.Declared && extra.VersionAt(at) is { Per: OperationsExtraPer.Item });
    }

    private static OperationsChargeLine? Freight(OperationsRatingInput input, OperationsRateCard card, OperationsPriceVersion version, OperationsRatingPiece piece, int number)
    {
        // An item price only on a list by size (ocean); air and ground always go by weight.
        if (card.PricesItems && version.PriceOf(piece.ItemTypeId) is { } price)
        {
            string? name = input.ItemTypes?.FirstOrDefault(type => type.Id == piece.ItemTypeId)?.Name;
            return new OperationsChargeLine(OperationsChargeLineKind.Item, name, number, 1, null, price, Money(price));
        }

        decimal basis = card.Basis == OperationsRateBasis.Volume ? BillableVolume(piece, input.DimensionUnit) : BillableWeight(piece, input.DivisorCm3PerKg, input.WeightUnit);
        OperationsRateBand? band = version.BandFor(basis);

        // Slice 4C, ground: a flat price per package, with the band or instead of it.
        decimal perPackage = version.PerPackage ?? 0m;

        if (band is null && perPackage == 0m)
        {
            return null;
        }
        decimal amount = (band?.Charge(basis) ?? 0m) + perPackage;

        // Per unit alone: the basis times the unit price; otherwise one charge, so quantity times unit price is still the amount.
        return band is { Kind: OperationsBandKind.PerUnit } && perPackage == 0m
            ? new OperationsChargeLine(OperationsChargeLineKind.Freight, null, number, basis, basis, band.Amount, Money(amount))
            : new OperationsChargeLine(OperationsChargeLineKind.Freight, null, number, 1, basis, Money(amount), Money(amount));
    }

    private static OperationsChargeLine? Extra(OperationsRatingInput input, OperationsChargeExtra extra, OperationsExtraVersion terms, decimal freight)
    {
        // Staff may give the quantity of an extra added by hand, or of one counted per item from the declaration ("some flexibility", owner).
        int? chosen = Adjustable(extra, input.At) && input.ExtraQuantities is { } quantities && quantities.TryGetValue(extra.Id, out int given) ? Math.Clamp(given, 0, 1000) : null;
        int pieces = input.Pieces.Count;
        int named = extra.Trigger == OperationsExtraTrigger.Declared ? input.Pieces.Count(piece => extra.IsNamedIn(piece.Contents)) : 0;

        int counted = extra.Trigger switch
        {
            OperationsExtraTrigger.Always => terms.Per == OperationsExtraPer.Shipment ? 1 : pieces,
            OperationsExtraTrigger.Declared => terms.Per == OperationsExtraPer.Shipment ? Math.Min(named, 1) : named,
            _ => 0
        };

        // A government fee is passed through: never fewer than the declaration counts (O-10).
        int quantity = chosen is { } typed ? (extra.IsGovernmentFee ? Math.Max(typed, counted) : typed) : counted;

        if (quantity <= 0 || pieces == 0)
        {
            return null;
        }

        return terms.Calculation == OperationsExtraCalculation.PercentOfFreight
            ? new OperationsChargeLine(OperationsChargeLineKind.Extra, extra.Name, null, 1, freight, terms.Amount, Money(freight * terms.Amount / 100m), extra.IsGovernmentFee, extra.Id)
            : new OperationsChargeLine(OperationsChargeLineKind.Extra, extra.Name, null, quantity, null, terms.Amount, Money(terms.Amount * quantity), extra.IsGovernmentFee, extra.Id);
    }

    private static decimal Money(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
