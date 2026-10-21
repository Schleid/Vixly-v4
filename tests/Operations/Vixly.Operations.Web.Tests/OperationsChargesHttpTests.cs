using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Web.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsChargesHttpTests.cs
//
// PURPOSE:
// Proves the prices and quotes pages through the staff site, on SQL Server (Slice 4, 4B and 4C).
//
// RESPONSIBILITIES:
// - A destination warehouse set on the warehouse form; prices one mode at a time: price lists per route with bands and scheduled versions, ground flat prices, item types on ocean, extras and insurance terms
// - A quote on a route uses the customer's own price list for it, the extra named in the declaration and the insurance; a preferred price is saved with its reason and never below government fees
// - The units lock once prices exist; a person without the permission sees no page in the menu and cannot change prices
// - Slice 5E: the supplies catalogue: a supply with barcodes, quantity and warehouse prices, a price for a later day, shelf labels; one code per item
// - Slice 5E step 2: the supplies stock: received once per form, adjusted with a reason, low stock, a transfer between warehouses, a count that corrects, the setting
//
// IMPORTANT NOTES:
// - The same test host and sign-in helpers as OperationsInspectionHttpTests
// =========================================================
public sealed partial class OperationsChargesHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsChargesHttpTests(OperationsWebDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        _host = await OperationsWebTestHost.StartAsync(_database.ConnectionString);
        _host.Devices.AutoApprove = true;
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [SqlServerFact]
    public async Task PricesByModeAndRoute_InsuranceTerms_AndAQuoteOnARoute_WithAPreferredPrice()
    {
        Guid tenant = NewTenant("priceco", "Price Co");
        using CookieClient owner = await OwnerAsync("priceco");
        await OfferAsync(tenant, PlatformShippingServices.AirStandard);
        await OfferAsync(tenant, PlatformShippingServices.OceanConsolidated);
        await OfferAsync(tenant, PlatformShippingServices.Ground);
        (Guid _, string valued) = await CustomerAsync(tenant, "valued@example.com");
        (Guid miami, Guid cayes, Guid haiti) = await PlacesAsync(tenant);

        // Jérémie, run by a partner, served through Les Cayes: set on the warehouse form.
        (HttpResponseMessage _, string warehouseForm) = await owner.GetAsync("/staff/locations/warehouse");
        Assert.Contains("name=\"deliversHome\"", warehouseForm, StringComparison.Ordinal);
        Assert.Equal("/staff/locations?saved=1", (await owner.PostAsync("/staff/locations/warehouse", warehouseForm, ("branchId", haiti.ToString()), ("code", "JER"), ("name", "Jérémie"),
            ("line1", "2 Rue"), ("city", "Jérémie"), ("countryCode", "HT"), ("roles", "PickupPoint"), ("status", "Active"), ("operatedBy", "Partner"), ("partnerName", "Grand'Anse Express"),
            ("hub", cayes.ToString()))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Give the partner’s name", (await owner.PostAsync("/staff/locations/warehouse", warehouseForm, ("branchId", haiti.ToString()), ("code", "MIR"),
            ("name", "Miragoâne"), ("line1", "3 Rue"), ("city", "Miragoâne"), ("countryCode", "HT"), ("roles", "PickupPoint"), ("status", "Active"), ("operatedBy", "Partner"))).Html,
            StringComparison.Ordinal);
        Guid jeremie = await WarehouseAsync(tenant, "JER");

        // One mode at a time: air shows no ocean item types; ocean does.
        (HttpResponseMessage _, string empty) = await owner.GetAsync("/staff/charges");
        Assert.Contains("No price list yet", empty, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/charges?mode=ocean\"", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("Ocean item types", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/zones\"", empty, StringComparison.Ordinal);
        Assert.Contains("data-add-band=", empty, StringComparison.Ordinal);
        (HttpResponseMessage _, string ocean) = await owner.GetAsync("/staff/charges?mode=ocean");
        Assert.Contains("Ocean item types", ocean, StringComparison.Ordinal);
        Assert.Equal("/staff/charges?mode=ocean&done=saved", (await owner.PostAsync("/staff/charges/item-types", ocean, ("name", "Barrel"), ("words", "barrel, drum"), ("active", "true")))
            .Response.Headers.Location?.OriginalString);

        // Air, standard for any route: 2.50 a kg, 2.25 from 20 kg; the valued customer's own list Miami → Jérémie, duties included.
        Assert.Equal("/staff/charges?mode=air&done=saved", (await owner.PostAsync("/staff/charges/cards", empty, List("Air", PlatformShippingServices.AirStandard, null, null, null,
            Band(0, "0", "PerUnit", "2.50"), Band(1, "20", "PerUnit", "2.25"), new[] { ("consolidation_fee", "5") }))).Response.Headers.Location?.OriginalString);
        Assert.Contains("A price list already exists for this service, route and customer", (await owner.PostAsync("/staff/charges/cards", empty,
            List("Air", PlatformShippingServices.AirStandard, null, null, null, Band(0, "0", "PerUnit", "3")))).Html, StringComparison.Ordinal);
        Assert.Contains("Check the bands: the first starts at 0", (await owner.PostAsync("/staff/charges/cards", empty,
            List("Air", PlatformShippingServices.AirStandard, miami, jeremie, null, Band(0, "1", "Flat", "3")))).Html, StringComparison.Ordinal);
        Assert.Contains("No customer has this code.", (await owner.PostAsync("/staff/charges/cards", empty,
            List("Air", PlatformShippingServices.AirStandard, null, null, "C99999X", Band(0, "0", "PerUnit", "2")))).Html, StringComparison.Ordinal);
        Assert.Contains("Choose the origin and destination from your warehouses.", (await owner.PostAsync("/staff/charges/cards", empty,
            List("Air", PlatformShippingServices.AirStandard, null, Guid.NewGuid(), null, Band(0, "0", "PerUnit", "2")))).Html, StringComparison.Ordinal);
        Assert.Equal("/staff/charges?mode=air&done=saved", (await owner.PostAsync("/staff/charges/cards", empty, List("Air", PlatformShippingServices.AirStandard, miami, jeremie, valued,
            Band(0, "0", "PerUnit", "2.00"), new[] { ("customs", "DutiesIncluded") }))).Response.Headers.Location?.OriginalString);

        // Ground: a flat price per package, no bands.
        (HttpResponseMessage _, string ground) = await owner.GetAsync("/staff/charges?mode=ground");
        Assert.Contains("name=\"per_package\"", ground, StringComparison.Ordinal);
        Assert.Equal("/staff/charges?mode=ground&done=saved", (await owner.PostAsync("/staff/charges/cards", ground, List("Ground", PlatformShippingServices.Ground, null, null, null,
            new[] { ("band_from_0", ""), ("per_package", "15") }))).Response.Headers.Location?.OriginalString);

        // Extras and insurance of the air tab.
        Assert.Equal("/staff/charges?mode=air&done=saved", (await owner.PostAsync("/staff/charges/extras", empty, ("name", "CONATEL fee"), ("government", "true"), ("amount", "10"),
            ("calculation", "Fixed"), ("per", "Item"), ("trigger", "Declared"), ("words", "iphone"), ("service", ""), ("country", "ht"), ("mode", "Air"), ("origin", ""), ("destination", ""),
            ("delivery", "None"), ("tab", "Air"), ("active", "true"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/staff/charges?mode=air&done=saved", (await owner.PostAsync("/staff/charges/insurance", empty, Insurance("Air", ("insurer_name", "Caribbean Cargo Insurance"),
            ("policy_number", "CC-2026")))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Check the insurance", (await owner.PostAsync("/staff/charges/insurance", empty, Insurance("Air", ("insurer_name", "")))).Html, StringComparison.Ordinal);
        (HttpResponseMessage _, string set) = await owner.GetAsync("/staff/charges?mode=air");
        Assert.Contains(valued + " Ana Pierre", set, StringComparison.Ordinal);
        Assert.Contains("Doral → Jérémie", set, StringComparison.Ordinal);
        Assert.Contains("Duties included", set, StringComparison.Ordinal);
        Assert.Contains("CONATEL fee", set, StringComparison.Ordinal);
        Assert.Contains("value=\"Caribbean Cargo Insurance\"", set, StringComparison.Ordinal);
        Assert.Contains("from 20: 2.25 USD / kg", set, StringComparison.Ordinal);
        Assert.DoesNotContain("<strong>CONATEL fee</strong>", (await owner.GetAsync("/staff/charges?mode=ocean")).Html, StringComparison.Ordinal);

        // New standard prices scheduled for later: shown as scheduled and removed; a day in the past refused.
        Guid standard = await StandardListAsync(tenant, PlatformShippingServices.AirStandard);
        string later = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("/staff/charges?mode=air&done=saved", (await owner.PostAsync("/staff/charges/cards", set, Version(standard, later, Band(0, "0", "PerUnit", "3.00"))))
            .Response.Headers.Location?.OriginalString);
        Assert.Contains("Choose a start date from today on.", (await owner.PostAsync("/staff/charges/cards", set,
            Version(standard, DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Band(0, "0", "PerUnit", "1")))).Html, StringComparison.Ordinal);
        (HttpResponseMessage _, string scheduled) = await owner.GetAsync("/staff/charges?mode=air");
        Assert.Contains("Scheduled", scheduled, StringComparison.Ordinal);
        Guid version = await ScheduledVersionAsync(tenant, standard);
        Assert.Equal("/staff/charges?mode=air&done=saved", (await owner.PostAsync("/staff/charges/versions/remove", scheduled, ("tab", "Air"), ("owner", "PriceList"),
            ("owner_id", standard.ToString()), ("version", version.ToString()))).Response.Headers.Location?.OriginalString);

        // Miami → Jérémie for the valued customer (2.00 a kg), the CONATEL fee named in the declaration, insurance at its minimum.
        (HttpResponseMessage _, string quotes) = await owner.GetAsync("/staff/quotes");
        Assert.Contains("<option value=\"" + jeremie + "\"", quotes, StringComparison.Ordinal);
        (HttpResponseMessage _, string priced) = await owner.PostAsync("/staff/quotes/price", quotes, Piece(valued, "", miami, jeremie).Append(("insured", "true")).ToArray());
        Assert.Contains("Freight, package 1 (13)", priced, StringComparison.Ordinal);
        Assert.Contains("26.00 USD", priced, StringComparison.Ordinal);
        Assert.Contains("CONATEL fee (government fee)", priced, StringComparison.Ordinal);
        Assert.Contains("41.00 USD", priced, StringComparison.Ordinal);
        Assert.Contains("Priced with this customer’s own price list.", priced, StringComparison.Ordinal);

        // The same customer to Les Cayes: the standard list; a walk-in to Jérémie with no origin given: the only origin, standard prices; home delivery is not offered at Jérémie.
        Assert.Contains("47.50 USD", (await owner.PostAsync("/staff/quotes/price", quotes, Piece(valued, "", miami, cayes).Append(("insured", "true")).ToArray())).Html,
            StringComparison.Ordinal);
        (HttpResponseMessage _, string walkIn) = await owner.PostAsync("/staff/quotes/price", quotes, Piece("", "", null, jeremie));
        Assert.Contains("42.50 USD", walkIn, StringComparison.Ordinal);
        Assert.DoesNotContain("Save the quote", walkIn, StringComparison.Ordinal);
        Assert.Contains("This delivery is not offered there.", (await owner.PostAsync("/staff/quotes/price", quotes, Piece("", "", null, jeremie).Append(("delivery", "HomeDelivery"))
            .ToArray())).Html, StringComparison.Ordinal);

        (HttpResponseMessage _, string tooLow) = await owner.PostAsync("/staff/quotes", priced, Piece(valued, PlatformShippingServices.AirStandard, miami, jeremie)
            .Concat(new[] { ("insured", "true"), ("preferred", "9"), ("reason", "Loyal"), ("key", Key()) }).ToArray());
        Assert.Contains("not below the government fees", tooLow, StringComparison.Ordinal);
        (string, string)[] save = Piece(valued, PlatformShippingServices.AirStandard, miami, jeremie).Concat(new[] { ("insured", "true"), ("preferred", "38"), ("reason", "Loyal customer"),
            ("days", "10"), ("key", Key()) }).ToArray();
        HttpResponseMessage saved = (await owner.PostAsync("/staff/quotes", priced, save)).Response;
        Assert.Matches("^/staff/quotes\\?done=saved&number=QT\\d{8}$", saved.Headers.Location?.OriginalString);
        Assert.Equal(saved.Headers.Location?.OriginalString, (await owner.PostAsync("/staff/quotes", priced, save)).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string listed) = await owner.GetAsync(saved.Headers.Location!.OriginalString);
        Assert.Contains("38.00 USD", listed, StringComparison.Ordinal);
        Assert.Contains("standard 41.00 USD", listed, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsQuote quote = await context.Quotes.SingleAsync();
            Assert.Equal((41m, 38m, "Loyal customer", 4, miami, jeremie), (quote.StandardTotal, quote.Total, quote.PreferredReason, quote.Lines.Count, quote.OriginWarehouseId!.Value,
                quote.DestinationWarehouseId!.Value));
            OperationsInsurancePlan plan = await context.InsurancePlans.SingleAsync();
            Assert.Equal((OperationsTransportMode.Air, OperationsInsuranceCover.Insurer, "CC-2026"), (plan.Mode, plan.Versions[^1].Coverage.Cover, plan.Versions[^1].Coverage.PolicyNumber));
            // The quotes list (owner, 2026-10-20): every quote with its status, by tab and by search; withdrawn from the list, it stays there.
            Assert.Contains("href=\"/staff/quotes/list\"", listed, StringComparison.Ordinal);
            (HttpResponseMessage _, string all) = await owner.GetAsync("/staff/quotes/list");
            Assert.Contains(quote.Number, all, StringComparison.Ordinal);
            Assert.Contains("Open (1)", all, StringComparison.Ordinal);
            Assert.Contains("data-status=\"open\"", all, StringComparison.Ordinal);
            Assert.Contains(quote.Number, (await owner.GetAsync("/staff/quotes/list?search=" + quote.Number.ToLowerInvariant())).Html, StringComparison.Ordinal);
            Assert.DoesNotContain(quote.Number, (await owner.GetAsync("/staff/quotes/list?search=QT00000000")).Html, StringComparison.Ordinal);
            Assert.DoesNotContain(quote.Number, (await owner.GetAsync("/staff/quotes/list?state=expired")).Html, StringComparison.Ordinal);
            string withdraw = "/staff/quotes/" + quote.Id + "/withdraw";
            Assert.Equal("/staff/quotes/list?state=withdrawn&done=withdrawn", (await owner.PostAsync(withdraw, all, ("from", "list"))).Response.Headers.Location?.OriginalString);
            (HttpResponseMessage _, string withdrawn) = await owner.GetAsync("/staff/quotes/list?state=withdrawn&done=withdrawn");
            Assert.Contains("The quote is withdrawn.", withdrawn, StringComparison.Ordinal);
            Assert.Contains(quote.Number, withdrawn, StringComparison.Ordinal);
            Assert.Contains("Withdrawn (1)", withdrawn, StringComparison.Ordinal);
            Assert.Equal("/staff/quotes", (await owner.PostAsync(withdraw, listed)).Response.Headers.Location?.OriginalString);
        }

        // Prices exist: the company's units are locked.
        (HttpResponseMessage _, string settings) = await owner.GetAsync("/staff/settings");
        Assert.Contains("The weight and size units cannot change once prices are set.", (await owner.PostAsync("/staff/settings", settings, Settings("Pound"))).Html, StringComparison.Ordinal);
        Assert.Contains("done=", (await owner.PostAsync("/staff/settings", settings, Settings("Kilogram", ("timeZone", "America/Port-au-Prince")))).Response.Headers.Location?.OriginalString,
            StringComparison.Ordinal);

        // Someone without the permissions: no menu entry, and the pages refuse to change prices.
        using CookieClient ada = await InspectorAsync(owner, "priceco");
        (HttpResponseMessage _, string adaHome) = await ada.GetAsync("/staff");
        Assert.DoesNotContain("href=\"/staff/charges\"", adaHome, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/quotes\"", adaHome, StringComparison.Ordinal);
        Assert.DoesNotMatch("QT\\d{8}", (await ada.GetAsync("/staff/quotes/list")).Html);
        (HttpResponseMessage _, string adaCharges) = await ada.GetAsync("/staff/charges");
        Assert.DoesNotContain("Add a price list", adaCharges, StringComparison.Ordinal);
        Assert.DoesNotContain("done=saved", (await ada.PostAsync("/staff/charges/cards", adaCharges,
            List("Air", PlatformShippingServices.AirStandard, null, cayes, null, Band(0, "0", "PerUnit", "1")))).Html, StringComparison.Ordinal);
        await using AsyncServiceScope check = _host.Services.CreateAsyncScope();
        check.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext data = check.ServiceProvider.GetRequiredService<OperationsDbContext>();
        Assert.Equal(3, await data.RateCards.CountAsync());
        Assert.Equal("America/Port-au-Prince", (await data.TenantSettings.SingleAsync()).TimeZone);
    }

    private static (string, string)[] Settings(string weight, params (string, string)[] more) =>
        new[] { ("currencyCode", "USD"), ("weightUnit", weight), ("dimensionUnit", "Centimetre"), ("pickup", "true"), ("shipmentPrefix", "SH"), ("receivingPrefix", "RC"),
            ("invoicePrefix", "INV"), ("paymentPrefix", "PAY"), ("customerPrefix", "C"), ("parcelPrefix", "PK") }.Concat(more).ToArray();

    private static (string, string)[] Band(int row, string from, string kind, string amount) =>
        new[] { ("band_from_" + row, from), ("band_kind_" + row, kind), ("band_amount_" + row, amount) };

    private static (string, string)[] List(string mode, string service, Guid? origin, Guid? destination, string? customer, params (string, string)[][] parts) =>
        new[] { ("mode", mode), ("service", service), ("origin", origin?.ToString() ?? string.Empty), ("destination", destination?.ToString() ?? string.Empty),
            ("customer", customer ?? string.Empty), ("customs", "TransportOnly"), ("active", "true") }
            .Where(field => parts.SelectMany(part => part).All(given => given.Item1 != field.Item1)).Concat(parts.SelectMany(part => part)).ToArray();

    private static (string, string)[] Version(Guid card, string effective, params (string, string)[][] parts) =>
        new[] { ("mode", "Air"), ("card", card.ToString()), ("effective", effective), ("active", "true") }.Concat(parts.SelectMany(part => part)).ToArray();

    private static (string, string)[] Insurance(string mode, params (string, string)[] more) =>
        new[] { ("mode", mode), ("method", "Percent"), ("percent", "3"), ("minimum", "5"), ("bands", ""), ("max_value", ""), ("cover", "Insurer"), ("risks", "Loss"), ("risks", "Damage"),
            ("exclusions", "CashAndDocuments"), ("payout", "LowerOfDeclaredAndInvoice"), ("damage_days", "7"), ("loss_days", "21"), ("proof", "Invoice"), ("filed_by", "Company"),
            ("active", "true") }.Concat(more).ToArray();

    private static (string, string)[] Piece(string customer, string service, Guid? origin, Guid destination) =>
        new[] { ("customer", customer), ("origin", origin?.ToString() ?? string.Empty), ("destination", destination.ToString()), ("service", service), ("weight_1", "12.3"),
            ("value_1", "100"), ("contents_1", "Two iPhones") };

    // Doral (Miami) receives; Les Cayes is a hub and pickup point delivering home; the Haiti branch holds them.
    // Slice 5E: the supplies catalogue under Prices.
    [SqlServerFact]
    public async Task Supplies_WithBarcodesAndPrices_OneCodePerItem_AndShelfLabels()
    {
        Guid tenant = NewTenant("supplyco", "Supply Co");
        using CookieClient owner = await OwnerAsync("supplyco");
        (Guid miami, Guid cayes, Guid _) = await PlacesAsync(tenant);

        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/supplies");
        Assert.Contains("href=\"/staff/supplies\"", list, StringComparison.Ordinal);
        Assert.Contains("No supply yet.", list, StringComparison.Ordinal);
        (string, string)[] box =
        {
            ("code", "box-m"), ("name_en", "Medium box"), ("name_fr", "Boîte moyenne"), ("category", "Boxes"), ("taxed", "true"), ("active", "true"),
            ("barcodes", "012345678905\n bx-m-2"), ("price", "2.50"), ("break_qty_1", "10"), ("break_price_1", "2.00"), ("wh_" + cayes, "3.00")
        };

        // Created, and the same form sent twice gives the same supply (ADR-0019).
        (HttpResponseMessage created, string _) = await owner.PostAsync("/staff/supplies", list, box);
        string path = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/supplies/[0-9a-f-]{36})\\?done=saved$").Groups["path"].Value;
        Assert.NotEmpty(path);
        Assert.StartsWith(path, (await owner.PostAsync("/staff/supplies", list, box)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        // One code per item: another supply cannot take its barcode; quantity prices must go down.
        Assert.Contains("A barcode is already used by another supply", WebUtility.HtmlDecode((await owner.PostAsync("/staff/supplies", list,
            ("code", "tape"), ("name_en", "Packing tape"), ("category", "Tape"), ("barcodes", "012345678905"), ("price", "4.00"))).Html), StringComparison.Ordinal);
        string refused = WebUtility.HtmlDecode((await owner.PostAsync("/staff/supplies", list,
            ("code", "tape"), ("name_en", "Packing tape"), ("category", "Tape"), ("price", "4.00"), ("break_qty_1", "6"), ("break_price_1", "4.50"))).Html);
        Assert.Contains("a price lower than the row before", refused, StringComparison.Ordinal);
        Assert.Contains("value=\"Packing tape\"", refused, StringComparison.Ordinal);

        (HttpResponseMessage _, string page) = await owner.GetAsync(path);
        string shown = WebUtility.HtmlDecode(page);
        Assert.Contains("BOX-M · Medium box", shown, StringComparison.Ordinal);
        Assert.Contains("10 or more: 2.00 USD", shown, StringComparison.Ordinal);
        Assert.Contains("CAY: 3.00 USD", shown, StringComparison.Ordinal);

        // A price from a later day, then removed before it starts.
        string later = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(path + "?done=price", (await owner.PostAsync(path + "/prices", page, ("price", "2.75"), ("effective", later))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string scheduled) = await owner.GetAsync(path);
        string remove = Regex.Match(scheduled, "action=\"(?<action>" + Regex.Escape(path) + "/prices/[0-9a-f-]{36}/remove)\"").Groups["action"].Value;
        Assert.NotEmpty(remove);
        Assert.Contains("new price planned", WebUtility.HtmlDecode((await owner.GetAsync("/staff/supplies")).Html), StringComparison.Ordinal);
        Assert.Equal(path + "?done=removed", (await owner.PostAsync(remove, scheduled)).Response.Headers.Location?.OriginalString);

        // The details change; a barcode is removed.
        Assert.Equal(path + "?done=saved", (await owner.PostAsync(path, page, ("code", "BOX-M"), ("name_en", "Medium box 40 cm"), ("category", "Boxes"), ("taxed", "true"),
            ("active", "true"), ("barcodes", "BX-M-2"))).Response.Headers.Location?.OriginalString);

        // A sheet of shelf labels.
        (HttpResponseMessage labels, string pdf) = await owner.GetRawAsync(path + "/labels?count=6");
        Assert.Equal("application/pdf", labels.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", pdf, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Supplies.OperationsSupply supply = await context.Supplies.Include(item => item.Barcodes).SingleAsync();
            Assert.Equal(("Medium box 40 cm", "BX-M-2"), (supply.NameEn, supply.Barcodes.Single().Code));
            Assert.Single(supply.Prices);
            Assert.Equal(((decimal?)2.50m, (decimal?)2.00m, (decimal?)3.00m), (supply.UnitPrice(null, 1, DateTimeOffset.UtcNow), supply.UnitPrice(miami, 12, DateTimeOffset.UtcNow),
                supply.UnitPrice(cayes, 12, DateTimeOffset.UtcNow)));
        }

        // Someone without the permission to set prices sees no supplies.
        using CookieClient ada = await InspectorAsync(owner, "supplyco");
        Assert.DoesNotContain("href=\"/staff/supplies\"", (await ada.GetAsync("/staff")).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Medium box", (await ada.GetAsync("/staff/supplies")).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Medium box", (await ada.GetAsync(path)).Html, StringComparison.Ordinal);
    }

    // Slice 5E step 2: the supplies stock per warehouse.
    [SqlServerFact]
    public async Task SuppliesStock_ReceivedAdjustedTransferredAndCounted_ByMovementsOnly()
    {
        Guid tenant = NewTenant("stockco", "Stock Co");
        using CookieClient owner = await OwnerAsync("stockco");
        (Guid miami, Guid cayes, Guid _) = await PlacesAsync(tenant);
        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/supplies");
        await owner.PostAsync("/staff/supplies", list, ("code", "TAPE"), ("name_en", "Packing tape"), ("category", "Tape"), ("taxed", "true"), ("active", "true"), ("price", "4.50"));
        Guid tape;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            tape = (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Supplies.SingleAsync()).Id;
        }

        // Received with its cost; the same form sent twice adds the stock once (ADR-0019).
        string here = "/staff/supplies/stock?warehouse=" + miami;
        (HttpResponseMessage _, string stock) = await owner.GetAsync(here);
        Assert.Contains("href=\"/staff/supplies/stock\"", stock, StringComparison.Ordinal);
        string key = Regex.Match(stock, "action=\"/staff/supplies/stock/receive\"[\\s\\S]*?name=\"key\" value=\"(?<key>[0-9a-f]{32})\"").Groups["key"].Value;
        (string, string)[] receipt = { ("warehouse", miami.ToString()), ("key", key), ("line_supply_1", tape.ToString()), ("line_qty_1", "40"), ("line_cost_1", "0.85"), ("supplier", "Uline"), ("reference", "INV-77") };
        Assert.Equal(here + "&done=received", (await owner.PostAsync("/staff/supplies/stock/receive", stock, receipt)).Response.Headers.Location?.OriginalString);
        Assert.Equal(here + "&done=received", (await owner.PostAsync("/staff/supplies/stock/receive", stock, receipt)).Response.Headers.Location?.OriginalString);

        // Adjusted: a correction needs a note; damaged does not. A reorder level shows low stock.
        Assert.Contains("a correction or “other” also needs a note", WebUtility.HtmlDecode((await owner.PostAsync("/staff/supplies/stock/adjust", stock, ("warehouse", miami.ToString()),
            ("key", Key()), ("supply", tape.ToString()), ("quantity", "1"), ("reason", "Correction"))).Html), StringComparison.Ordinal);
        Assert.Equal(here + "&done=adjusted", (await owner.PostAsync("/staff/supplies/stock/adjust", stock, ("warehouse", miami.ToString()), ("key", Key()), ("supply", tape.ToString()),
            ("quantity", "-2"), ("reason", "Damaged"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(here + "&done=reorder", (await owner.PostAsync("/staff/supplies/stock/reorder", stock, ("warehouse", miami.ToString()), ("supply", tape.ToString()), ("level", "30")))
            .Response.Headers.Location?.OriginalString);

        // Ten sent to Les Cayes: they leave Miami now and arrive when received there.
        Assert.Equal(here + "&done=sent", (await owner.PostAsync("/staff/supplies/stock/transfers", stock, ("warehouse", miami.ToString()), ("key", Key()), ("to", cayes.ToString()),
            ("line_supply_1", tape.ToString()), ("line_qty_1", "10"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string atCayes) = await owner.GetAsync("/staff/supplies/stock?warehouse=" + cayes);
        string receive = Regex.Match(atCayes, "action=\"(?<action>/staff/supplies/stock/transfers/[0-9a-f-]{36}/receive)\"").Groups["action"].Value;
        Assert.NotEmpty(receive);
        (HttpResponseMessage _, string atMiami) = await owner.GetAsync(here);
        Assert.Contains("1 supplies are at or below their reorder level.", WebUtility.HtmlDecode(atMiami), StringComparison.Ordinal);
        Assert.Contains("TAPE · Packing tape: 28 left (reorder at 30)", WebUtility.HtmlDecode((await owner.GetAsync("/staff")).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/supplies/stock?done=arrived", (await owner.PostAsync(receive, atCayes)).Response.Headers.Location?.OriginalString);
        Assert.Contains("This transfer was already received.", WebUtility.HtmlDecode((await owner.PostAsync(receive, atCayes)).Html), StringComparison.Ordinal);

        // A count at Miami finds 25 of the 28 on hand: closing corrects by -3.
        (HttpResponseMessage opened, string _) = await owner.PostAsync("/staff/supplies/stock/counts", atMiami, ("warehouse", miami.ToString()), ("category", ""));
        string countPath = opened.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Matches("^/staff/supplies/stock/counts/[0-9a-f-]{36}$", countPath);
        (HttpResponseMessage _, string countPage) = await owner.GetAsync(countPath);
        Assert.Equal(countPath + "?done=closed", (await owner.PostAsync(countPath, countPage, ("count_" + tape, "25"), ("action", "close"))).Response.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string movements) = await owner.GetAsync("/staff/supplies/" + tape + "/movements?warehouse=" + miami);
        string shown = WebUtility.HtmlDecode(movements);
        Assert.Contains("On hand now: 25", shown, StringComparison.Ordinal);
        Assert.Contains("Uline · INV-77 · 0.85 USD", shown, StringComparison.Ordinal);
        Assert.Contains("Count correction", shown, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Assert.Equal(new[] { -10, -3, -2, 40 }, (await context.SupplyMovements.Where(item => item.WarehouseId == miami).Select(item => item.Quantity).ToListAsync()).OrderBy(quantity => quantity));
            Assert.Equal((25, 10), ((await context.SupplyStocks.SingleAsync(item => item.WarehouseId == miami)).OnHand, (await context.SupplyStocks.SingleAsync(item => item.WarehouseId == cayes)).OnHand));
        }

        // The company stops counting its stock: the actions are refused.
        (HttpResponseMessage _, string supplies) = await owner.GetAsync("/staff/supplies");
        Assert.Equal("/staff/supplies?done=saved", (await owner.PostAsync("/staff/supplies/settings", supplies, ("stock_out", "Refuse"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("does not count its supplies stock", (await owner.GetAsync(here)).Html, StringComparison.Ordinal);
        Assert.Contains("The company does not count its supplies stock.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/supplies/stock/adjust", stock, ("warehouse", miami.ToString()),
            ("key", Key()), ("supply", tape.ToString()), ("quantity", "1"), ("reason", "Found"))).Html), StringComparison.Ordinal);

        // Someone without the right sees no stock.
        using CookieClient ada = await InspectorAsync(owner, "stockco");
        Assert.DoesNotContain("href=\"/staff/supplies/stock\"", (await ada.GetAsync("/staff")).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Packing tape", (await ada.GetAsync(here)).Html, StringComparison.Ordinal);
    }

    private async Task<(Guid Miami, Guid Cayes, Guid Haiti)> PlacesAsync(Guid tenant)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationsBranch florida = OperationsBranch.Create(tenant, Place("MIA", "Miami", "Miami", "US"), OperationsBranchSide.Origin, now);
        OperationsBranch haiti = OperationsBranch.Create(tenant, Place("HT", "Haiti", "Les Cayes", "HT"), OperationsBranchSide.Destination, now);
        OperationsWarehouse doral = OperationsWarehouse.Create(tenant, florida.Id, Place("DOR", "Doral", "Doral", "US"), OperationsWarehouseRoles.Receiving, true, now);
        OperationsWarehouse cayes = OperationsWarehouse.Create(tenant, haiti.Id, Place("CAY", "Les Cayes", "Les Cayes", "HT"),
            OperationsWarehouseRoles.DispatchHub | OperationsWarehouseRoles.PickupPoint, false, now);
        cayes.ChangeDestination(new OperationsDestinationTerms(true, null, OperationsDestinationOperator.Company, null, null, null, null), now);
        context.AddRange(florida, haiti, doral, cayes);
        await context.SaveChangesAsync();
        return (doral.Id, cayes.Id, haiti.Id);
    }

    private static OperationsLocationDetails Place(string code, string name, string city, string country) =>
        new(code, name, OperationsPostalAddress.Create("1 Main", null, city, country == "US" ? "FL" : null, country == "US" ? "33122" : null, country), null, null, null, false);

    private async Task<Guid> WarehouseAsync(Guid tenant, string code)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Warehouses.SingleAsync(warehouse => warehouse.Code == code)).Id;
    }

    private async Task<Guid> StandardListAsync(Guid tenant, string service)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().RateCards
            .SingleAsync(card => card.ServiceCode == service && card.CustomerId == null && card.OriginWarehouseId == null && card.DestinationWarehouseId == null)).Id;
    }

    private async Task<Guid> ScheduledVersionAsync(Guid tenant, Guid card)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsRateCard found = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().RateCards.SingleAsync(item => item.Id == card);
        return found.Versions.Single(item => item.EffectiveFromUtc > DateTimeOffset.UtcNow).Id;
    }

    private async Task OfferAsync(Guid tenant, string service)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsShippingServiceSetup setup = OperationsShippingServiceSetup.Create(tenant, service, DateTimeOffset.UtcNow);
        setup.Change(true, null, 4, 7, 6000m, true, DateTimeOffset.UtcNow);
        scope.ServiceProvider.GetRequiredService<IOperationsShippingServiceRepository>().Add(setup);
        await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    private static string Key() => Guid.NewGuid().ToString("N");

    private static string Code(string manualKey, DateTime at) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(at);

    private async Task<CookieClient> InspectorAsync(CookieClient owner, string slug)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, ("name", "Inspector"), ("permissions", "ops.parcels.view"), ("permissions", "ops.inspection.check"));
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Inspector</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", "Ada Lovelace"), ("email", "ada@" + slug + ".example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));

        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient ada = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await ada.GetAsync(new Uri(link).PathAndQuery);
        await ada.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(ada);
        return ada;
    }

    private async Task<(Guid Id, string Code)> CustomerAsync(Guid tenant, string email)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        string code = "C" + Random.Shared.Next(10000, 99999).ToString(System.Globalization.CultureInfo.InvariantCulture);
        OperationsCustomer customer = OperationsCustomer.SignUp(tenant, "Ana", "Pierre", null, EmailAddress.Create(email), "+50937123456", LanguageCode.En, "$2a$12$hash", DateTimeOffset.UtcNow);
        customer.ConfirmEmail(code, DateTimeOffset.UtcNow);
        scope.ServiceProvider.GetRequiredService<IOperationsCustomerRepository>().Add(customer);
        await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        return (customer.Id, code);
    }

    private Guid NewTenant(string slug, string company)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, "active");
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, "en", "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    private static async Task EnrolAsync(CookieClient browser)
    {
        (HttpResponseMessage _, string page) = await browser.GetAsync("/devices/enrol");
        await browser.PostAsync("/devices/enrol", page, ("name", "Laptop"), ("kind", "pc"));
        Assert.NotNull(browser.Cookie(OperationsCookies.Device));
    }

    private async Task<CookieClient> OwnerAsync(string slug)
    {
        Guid tenant = _host.Platform.Portals[slug].TenantId!.Value;
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(ManualKey().Match(enrol).Groups["key"].Value, DateTime.UtcNow)));
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
        await EnrolAsync(browser);
        return browser;
    }


    [GeneratedRegex("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant)]
    private static partial Regex ManualKey();
}
