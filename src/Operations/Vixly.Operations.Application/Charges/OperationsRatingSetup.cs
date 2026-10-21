using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRatingSetup.cs
//
// PURPOSE:
// The company's prices loaded once for a rating: settings, services, price lists, extras, insurance, zones and item types (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Rate one service, or every service the company offers with a price, for a zone, at a moment
// - Turn typed pieces into kilograms and centimetres
//
// IMPORTANT NOTES:
// - A service the company does not offer, or without a price list in force, has no price
// =========================================================
public sealed record OperationsRatingSetup(
    OperationsTenantSettings Settings,
    IReadOnlyList<OperationsShippingServiceSetup> Services,
    IReadOnlyList<OperationsRateCard> Cards,
    IReadOnlyList<OperationsChargeExtra> Extras,
    IReadOnlyList<OperationsInsurancePlan> Insurance,
    IReadOnlyList<OperationsWarehouse> Warehouses,
    IReadOnlyList<OperationsItemType> ItemTypes)
{
    // Bounds that keep every amount inside the columns: 50 pieces of 10,000 at 1,000,000 a unit.
    public const decimal MaxWeight = 10_000m;

    public const decimal MaxLength = 2_000m;

    public bool Offers(string serviceCode) => Services.Any(service => service.ServiceCode == serviceCode && service.Offered);

    public string? Label(string serviceCode) => Services.FirstOrDefault(service => service.ServiceCode == serviceCode)?.Label;

    public decimal? Divisor(string serviceCode) =>
        Services.FirstOrDefault(service => service.ServiceCode == serviceCode)?.VolumetricDivisor ?? PlatformShippingServices.Find(serviceCode)?.DefaultVolumetricDivisor;

    // Slice 4C: the company's destinations (pickup there or home delivery from there) and origins (drop-off), in use.
    public IReadOnlyList<OperationsWarehouse> Destinations => Warehouses.Where(warehouse => warehouse.IsDestination).OrderBy(warehouse => warehouse.Name, StringComparer.CurrentCulture).ToList();

    public IReadOnlyList<OperationsWarehouse> Origins => Warehouses.Where(warehouse => warehouse.IsOrigin).OrderBy(warehouse => warehouse.Name, StringComparer.CurrentCulture).ToList();

    public OperationsWarehouse? Destination(Guid? warehouseId) => warehouseId is { } id ? Warehouses.FirstOrDefault(warehouse => warehouse.Id == id && warehouse.IsDestination) : null;

    public OperationsWarehouse? Origin(Guid? warehouseId) => warehouseId is { } id ? Warehouses.FirstOrDefault(warehouse => warehouse.Id == id && warehouse.IsOrigin) : null;

    // Slice 4C: a route of the company: to one of its destinations, from the origin given (else its only origin, else any).
    public bool TryRoute(Guid? originId, Guid? destinationId, out OperationsWarehouse? from, out OperationsWarehouse to)
    {
        IReadOnlyList<OperationsWarehouse> origins = Origins;
        from = originId is not null ? Origin(originId) : origins.Count == 1 ? origins[0] : null;
        to = Destination(destinationId)!;
        return to is not null && (originId is null || from is not null);
    }

    // Slice 5D: the product chosen when it is for this mode; else the mode's first product in use (quotes, the calculator, shipments from before products).
    public OperationsInsurancePlan? InsuranceFor(OperationsTransportMode mode, Guid? planId = null) =>
        planId is { } id
            ? Insurance.FirstOrDefault(plan => plan.Id == id && plan.Mode == mode)
            : Insurance.Where(plan => plan.Mode == mode).OrderByDescending(plan => plan.IsActive).ThenBy(plan => plan.Position).ThenBy(plan => plan.Name, StringComparer.Ordinal).FirstOrDefault();

    public OperationsRating? Rate(string serviceCode, OperationsWarehouse? origin, OperationsWarehouse? destination, Guid? customerId, IReadOnlyList<OperationsRatingPiece> pieces,
        bool consolidated, bool insured, DateTimeOffset at, OperationsDeliveryModes delivery = OperationsDeliveryModes.None, IReadOnlyDictionary<Guid, int>? extraQuantities = null,
        Guid? insurancePlanId = null)
    {
        ArgumentNullException.ThrowIfNull(pieces);

        return OperationsRateCardChoice.Pick(Cards, serviceCode, origin?.Id, destination?.Id, customerId, at) is { } card && (card.CustomerId is null || card.CustomerId == customerId)
            ? OperationsRatingEngine.Rate(new OperationsRatingInput(card, Extras.Where(extra => extra.IsActive).ToList(), InsuranceFor(card.Mode, insurancePlanId), Divisor(serviceCode),
                Settings.WeightUnit, Settings.DimensionUnit, destination?.Address.CountryCode, origin?.Id, destination?.Id, delivery, pieces, consolidated, insured, at, ItemTypes,
                extraQuantities))
            : null;
    }

    // Every service the company offers and has a price for on the route, in the catalog's order.
    public IReadOnlyList<OperationsRating> RateOffered(OperationsWarehouse? origin, OperationsWarehouse? destination, Guid? customerId, IReadOnlyList<OperationsRatingPiece> pieces,
        bool consolidated, bool insured, DateTimeOffset at, OperationsDeliveryModes delivery = OperationsDeliveryModes.None, IReadOnlyDictionary<Guid, int>? extraQuantities = null) =>
        PlatformShippingServices.All.Where(service => Offers(service.Code))
            .Select(service => Rate(service.Code, origin, destination, customerId, pieces, consolidated, insured, at, delivery, extraQuantities))
            .OfType<OperationsRating>().ToList();

    // Null when the typed values cannot be a piece: a weight above zero, sizes and value not negative, an item type in use.
    public OperationsRatingPiece? Piece(OperationsPieceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Weight is not (> 0 and <= MaxWeight) || input.Length is < 0 or > MaxLength || input.Width is < 0 or > MaxLength || input.Height is < 0 or > MaxLength
            || input.Value is < 0 or > OperationsChargeLimits.MaxAmount || (input.ItemTypeId is { } typeId && !ItemTypes.Any(type => type.Id == typeId && type.IsActive)))
        {
            return null;
        }

        string? contents = string.IsNullOrWhiteSpace(input.Contents) ? null : input.Contents.Trim();

        return contents is { Length: > OperationsChargeLimits.WordsMaxLength }
            ? null
            : new OperationsRatingPiece(
                OperationsUnits.ToKilograms(input.Weight.Value, Settings.WeightUnit),
                OperationsUnits.ToCentimetres(input.Length ?? 0m, Settings.DimensionUnit),
                OperationsUnits.ToCentimetres(input.Width ?? 0m, Settings.DimensionUnit),
                OperationsUnits.ToCentimetres(input.Height ?? 0m, Settings.DimensionUnit),
                input.Value ?? 0m,
                input.ItemTypeId,
                contents);
    }

    public IReadOnlyList<OperationsRatingPiece>? Pieces(IReadOnlyList<OperationsPieceInput>? inputs)
    {
        if (inputs is not { Count: > 0 and <= OperationsChargeLimits.MaxPieces })
        {
            return null;
        }

        List<OperationsRatingPiece> pieces = inputs.Select(Piece).OfType<OperationsRatingPiece>().ToList();
        return pieces.Count == inputs.Count ? pieces : null;
    }
}
