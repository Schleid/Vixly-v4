using Vixly.Operations.Domain.Bundles;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchCost.cs
//
// PURPOSE:
// Works out a dispatch's carrier cost and each shipment's share (Slice 6B step 2, D-7; owner, A-2 and A-6).
//
// RESPONSIBILITIES:
// - Pick the carrier's rate in force for the destination on a day
// - Air: the bundles' chargeable weight times the rate, at least the minimum; ocean and ground: each bundle at its type's rate
// - Share a total by chargeable weight, to the cent, the rounding on the largest share
//
// IMPORTANT NOTES:
// - A rate for the destination warehouse wins over one for its country; the latest start on or before the day wins
// - No rate: no cost for that leg
// =========================================================
public static class OperationsDispatchCost
{
    // The rate in force: for the warehouse before the country, for the bundle type before any type, the latest start on or before the day.
    public static OperationsCarrierRate? RateFor(IEnumerable<OperationsCarrierRate> rates, OperationsDispatch dispatch, string? bundleType, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(dispatch);
        return rates
            .Where(rate => rate.StartsOn <= day && rate.CountryCode == dispatch.DestinationCountryCode
                && (rate.DestinationWarehouseId is null || rate.DestinationWarehouseId == dispatch.DestinationWarehouseId)
                && (rate.BundleType is null || string.Equals(rate.BundleType, bundleType, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(rate => rate.DestinationWarehouseId is not null)
            .ThenByDescending(rate => rate.BundleType is not null)
            .ThenByDescending(rate => rate.StartsOn)
            .FirstOrDefault();
    }

    public static decimal ChargeableKg(OperationsBundle bundle) =>
        Math.Max((bundle ?? throw new ArgumentNullException(nameof(bundle))).WeightKg ?? bundle.ParcelsWeightKg, bundle.VolumetricKg ?? 0m);

    // One leg's cost with its carrier's rates; none when a bundle has no rate.
    public static decimal? LegCost(OperationsCarrier carrier, IReadOnlyCollection<OperationsCarrierRate> rates, OperationsDispatch dispatch, IReadOnlyCollection<OperationsBundle> bundles, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(carrier);
        ArgumentNullException.ThrowIfNull(bundles);

        if (bundles.Count == 0)
        {
            return null;
        }

        if (carrier.Kind == OperationsCarrierKind.Air)
        {
            if (RateFor(rates, dispatch, null, day) is not { PerKg: { } perKg } rate)
            {
                return null;
            }

            decimal amount = decimal.Round(bundles.Sum(ChargeableKg) * perKg, 2, MidpointRounding.AwayFromZero);
            return decimal.Round(Math.Max(amount, rate.Minimum ?? 0m), 2, MidpointRounding.AwayFromZero);
        }

        decimal total = 0m;

        foreach (OperationsBundle bundle in bundles)
        {
            if (RateFor(rates, dispatch, bundle.TypeName, day) is not { PerBundle: { } perBundle })
            {
                return null;
            }

            total += perBundle;
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    // The total shared by weight: each share rounded down to the cent, the cents left over one each to the largest remainders,
    // so the shares add up to the total and none is negative (review).
    public static IReadOnlyList<decimal> Share(decimal total, IReadOnlyList<decimal> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        decimal sum = weights.Sum();

        if (weights.Count == 0)
        {
            return Array.Empty<decimal>();
        }

        List<decimal> exact = weights.Select(weight => sum > 0m ? total * weight / sum : total / weights.Count).ToList();
        List<decimal> shares = exact.Select(value => decimal.Floor(value * 100m) / 100m).ToList();
        int cents = (int)decimal.Round((total - shares.Sum()) * 100m);

        foreach (int index in exact.Select((value, index) => (Remainder: value - shares[index], index)).OrderByDescending(item => item.Remainder).ThenBy(item => item.index)
            .Take(Math.Max(0, cents)).Select(item => item.index).ToList())
        {
            shares[index] += 0.01m;
        }

        return shares;
    }
}
