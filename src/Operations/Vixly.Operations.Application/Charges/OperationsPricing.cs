using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsPricing.cs
//
// PURPOSE:
// The prices a pricing page shows (owner, 2026-10-21): on the customer portal, the regular prices and the customer's own; on the public website, the lists the company publishes.
//
// RESPONSIBILITIES:
// - The price lists in use with a version in force now, for the services the company offers
// - The route's names, the items' names, the company's units and currency
//
// IMPORTANT NOTES:
// - Never another customer's prices; the public website never shows a customer's prices
// - The pricing engine is unchanged: these are the same lists it rates with
// =========================================================
public sealed class OperationsPricing
{
    private readonly OperationsRater _rater;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsChargeRepository _charges;

    public OperationsPricing(OperationsRater rater, IOperationsSettingsRepository settings, IOperationsChargeRepository charges)
    {
        _rater = rater ?? throw new ArgumentNullException(nameof(rater));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
    }

    // The website's menu: two small reads, never the prices themselves.
    public async Task<bool> PublicShownAsync(CancellationToken cancellationToken) =>
        (await _settings.FindAsync(cancellationToken))?.PublicPricing == true && await _charges.AnyPublishedAsync(cancellationToken);

    // A signed-in customer: the regular prices and theirs.
    public async Task<OperationsPricingView> ForCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
        View(await _rater.LoadAsync(tenantId, customerId, cancellationToken), card => card.CustomerId is null || card.CustomerId == customerId);

    // The public website: the regular lists the company publishes, while it publishes prices; none otherwise.
    public async Task<OperationsPricingView?> PublicAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        OperationsRatingSetup setup = await _rater.LoadAsync(tenantId, null, cancellationToken);
        return setup.Settings.PublicPricing ? View(setup, card => card.CustomerId is null && card.Published) : null;
    }

    private OperationsPricingView View(OperationsRatingSetup setup, Func<OperationsRateCard, bool> shown)
    {
        DateTimeOffset utcNow = _rater.UtcNow;
        Dictionary<Guid, string> places = setup.Warehouses.ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Name);
        Dictionary<Guid, string> items = setup.ItemTypes.ToDictionary(item => item.Id, item => item.Name);
        List<OperationsPriceListView> lists = new();

        foreach (OperationsRateCard card in setup.Cards.Where(card => card.IsActive && shown(card) && setup.Offers(card.ServiceCode)))
        {
            if (card.VersionAt(utcNow) is not { } version)
            {
                continue;
            }

            lists.Add(new OperationsPriceListView(card.ServiceCode, setup.Label(card.ServiceCode),
                card.OriginWarehouseId is { } origin ? places.GetValueOrDefault(origin) : null, card.DestinationWarehouseId is { } destination ? places.GetValueOrDefault(destination) : null,
                card.CustomerId is not null, card.Basis, card.Mode, OperationsTimeZones.Today(version.EffectiveFromUtc, setup.Settings.TimeZone), version.Bands,
                version.Items.Where(item => items.ContainsKey(item.ItemTypeId)).Select(item => new OperationsPriceItemView(items[item.ItemTypeId], item.Price)).ToList(),
                version.MinimumCharge, version.ServiceFee, version.ConsolidationFee, version.PerPackage, version.PerShipment));
        }

        return new OperationsPricingView(lists.OrderByDescending(list => list.Own).ThenBy(list => list.Mode).ThenBy(list => list.ServiceCode, StringComparer.Ordinal)
            .ThenBy(list => list.Origin, StringComparer.CurrentCulture).ThenBy(list => list.Destination, StringComparer.CurrentCulture).ToList(),
            setup.Settings.WeightUnit, setup.Settings.DimensionUnit, setup.Settings.CurrencyCode);
    }
}
