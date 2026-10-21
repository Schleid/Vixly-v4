using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsDispatchCosting.cs
//
// PURPOSE:
// Works out a dispatch's carrier cost and shares it among its shipments (Slice 6B step 2, D-7).
//
// RESPONSIBILITIES:
// - Each leg with a carrier: its rate in force on the departure day for the destination
// - The total on the dispatch; each shipment's share by chargeable weight
//
// IMPORTANT NOTES:
// - A leg whose carrier has no rate leaves the cost unknown; the dispatcher can type it with a note
// - The caller saves
// =========================================================
public sealed class OperationsDispatchCosting
{
    private readonly IOperationsDispatchRepository _dispatches;

    public OperationsDispatchCosting(IOperationsDispatchRepository dispatches)
    {
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
    }

    // The legs with a carrier but no rate; none when the cost is known.
    public async Task<IReadOnlyList<int>> CostAsync(OperationsDispatch dispatch, OperationsTenantSettings settings, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(settings);
        DateOnly day = OperationsTimeZones.Today(dispatch.DepartedUtc ?? utcNow, settings.TimeZone);
        IReadOnlyList<OperationsBundle> bundles = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);
        decimal total = 0m;
        List<int> missing = new();
        bool any = false;

        // A rate is for the whole trip to the destination: each carrier is counted once, on its first leg; a carrier with no rates at all
        // (the company's own truck) costs nothing here (review).
        foreach (OperationsDispatchLeg leg in dispatch.Legs.Where(leg => leg.CarrierId is not null).OrderBy(leg => leg.Sequence).DistinctBy(leg => leg.CarrierId))
        {
            OperationsCarrier? carrier = await _dispatches.GetCarrierAsync(leg.CarrierId!.Value, cancellationToken);
            IReadOnlyList<OperationsCarrierRate> rates = carrier is null ? Array.Empty<OperationsCarrierRate>() : await _dispatches.ListRatesAsync(carrier.Id, cancellationToken);

            if (carrier is null || rates.Count == 0)
            {
                continue;
            }

            decimal? cost = OperationsDispatchCost.LegCost(carrier, rates, dispatch, bundles, day);

            if (cost is { } amount)
            {
                (total, any) = (total + amount, true);
            }
            else
            {
                missing.Add(leg.Sequence);
            }
        }

        dispatch.Costed(any && missing.Count == 0 ? total : null, settings.CurrencyCode, utcNow);
        await ShareAsync(dispatch, utcNow, cancellationToken);
        return missing;
    }

    public async Task ShareAsync(OperationsDispatch dispatch, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        List<OperationsDispatchShipment> rows = (await _dispatches.ListDispatchShipmentsAsync(dispatch.Id, cancellationToken)).OrderBy(row => row.HouseNumber, StringComparer.Ordinal).ToList();

        if (dispatch.CostTotal is not { } total)
        {
            rows.ForEach(row => row.Share(null, utcNow));
            return;
        }

        IReadOnlyList<decimal> shares = OperationsDispatchCost.Share(total, rows.Select(row => row.ChargeableKg).ToList());

        for (int index = 0; index < rows.Count; index++)
        {
            rows[index].Share(shares[index], utcNow);
        }
    }
}
