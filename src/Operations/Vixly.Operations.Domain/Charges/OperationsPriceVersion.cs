using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsPriceVersion.cs
//
// PURPOSE:
// One dated version of a price list: its bands, item prices and fees (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Check and keep the bands, item prices, minimum, service and consolidation fees
// - Find the band of a billable weight or size, and an item's price
//
// IMPORTANT NOTES:
// - Stored inside the price list (JSON)
// - The first band starts at 0; bands are in order and start once each
// =========================================================
public sealed class OperationsPriceVersion : IOperationsVersion
{
    private readonly List<OperationsRateBand> _bands = new();

    private readonly List<OperationsRateItem> _items = new();

    private OperationsPriceVersion()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public IReadOnlyList<OperationsRateBand> Bands => _bands;

    public IReadOnlyList<OperationsRateItem> Items => _items;

    public decimal? MinimumCharge { get; private set; }

    public decimal? ServiceFee { get; private set; }

    public decimal? ConsolidationFee { get; private set; }

    // Slice 4C, ground: a flat price per package (with the bands or instead of them) and per shipment.
    public decimal? PerPackage { get; private set; }

    public decimal? PerShipment { get; private set; }

    public static OperationsPriceVersion Create(OperationsRatePrices prices, DateTimeOffset effectiveFromUtc)
    {
        ArgumentNullException.ThrowIfNull(prices);
        List<OperationsRateBand> bands = (prices.Bands ?? Array.Empty<OperationsRateBand>()).OrderBy(band => band.From).Select(Checked).ToList();

        // Bands may be left out only when a flat price above zero stands in for them (ground).
        bool flat = prices.PerPackage is > 0m || prices.PerShipment is > 0m;

        if ((bands.Count == 0 && !flat) || bands.Count > OperationsChargeLimits.MaxBands || (bands.Count > 0 && bands[0].From != 0m)
            || bands.Select(band => band.From).Distinct().Count() != bands.Count)
        {
            throw new DomainException(OperationsChargeErrors.BandsInvalid);
        }

        List<OperationsRateItem> items = (prices.Items ?? Array.Empty<OperationsRateItem>())
            .Select(item => new OperationsRateItem(item.ItemTypeId, OperationsChargeLimits.Money(item.Price, OperationsChargeErrors.ItemsInvalid))).ToList();

        if (items.Count > OperationsChargeLimits.MaxItemTypes || items.Any(item => item.ItemTypeId == Guid.Empty) || items.Select(item => item.ItemTypeId).Distinct().Count() != items.Count)
        {
            throw new DomainException(OperationsChargeErrors.ItemsInvalid);
        }

        OperationsPriceVersion version = new()
        {
            Id = Guid.NewGuid(),
            EffectiveFromUtc = effectiveFromUtc,
            MinimumCharge = OperationsChargeLimits.Money(prices.MinimumCharge, OperationsChargeErrors.PriceInvalid),
            ServiceFee = OperationsChargeLimits.Money(prices.ServiceFee, OperationsChargeErrors.PriceInvalid),
            ConsolidationFee = OperationsChargeLimits.Money(prices.ConsolidationFee, OperationsChargeErrors.PriceInvalid),
            PerPackage = OperationsChargeLimits.Money(prices.PerPackage, OperationsChargeErrors.PriceInvalid),
            PerShipment = OperationsChargeLimits.Money(prices.PerShipment, OperationsChargeErrors.PriceInvalid)
        };
        version._bands.AddRange(bands);
        version._items.AddRange(items);
        return version;
    }

    // Null when the version has flat prices only.
    public OperationsRateBand? BandFor(decimal billable) => _bands.Where(band => band.From <= billable).OrderByDescending(band => band.From).FirstOrDefault();

    public decimal? PriceOf(Guid? itemTypeId) => itemTypeId is { } id ? _items.FirstOrDefault(item => item.ItemTypeId == id)?.Price : null;

    private static OperationsRateBand Checked(OperationsRateBand band)
    {
        if (!Enum.IsDefined(band.Kind) || band.From is < 0 or > OperationsChargeLimits.MaxAmount || decimal.Round(band.From, 2) != band.From
            || (band.Kind == OperationsBandKind.BasePlusPerUnit) != (band.Base is not null))
        {
            throw new DomainException(OperationsChargeErrors.BandsInvalid);
        }

        decimal amount = band.Kind == OperationsBandKind.Flat
            ? OperationsChargeLimits.Money(band.Amount, OperationsChargeErrors.BandsInvalid)
            : OperationsChargeLimits.UnitPrice(band.Amount, OperationsChargeErrors.BandsInvalid);
        return new OperationsRateBand(band.From, band.Kind, amount, OperationsChargeLimits.Money(band.Base, OperationsChargeErrors.BandsInvalid));
    }
}
