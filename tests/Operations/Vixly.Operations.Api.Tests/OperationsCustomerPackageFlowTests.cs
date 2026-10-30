using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Operations;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: OperationsCustomerPackageFlowTests.cs
//
// PURPOSE:
// Proves the customer's package pages end to end: the real portal, the real Operations.Api and a real database (Slice 3B, design section 7).
//
// RESPONSIBILITIES:
// - Announce a package, refuse a second announcement, withdraw one
// - A package that arrived first takes the customer's announcement and leaves the "awaiting information" hold
// - A customer sees only their own packages and photos, with customer wording and no staff details
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER
// - The received package is seeded as receiving would leave it; the staff side is proven by OperationsParcelHttpTests
// =========================================================
public sealed partial class OperationsCustomerPackageFlowTests : IClassFixture<OperationsApiDatabase>, IAsyncLifetime
{
    private const string Password = "Correct horse 42";

    private readonly OperationsApiDatabase _database;

    private readonly Guid _tenant = Guid.NewGuid();

    private OperationsApiTestHost _api = null!;

    private WebApplication _portal = null!;

    private string _slug = string.Empty;

    private Guid _warehouse;

    private Guid _branch;

    public OperationsCustomerPackageFlowTests(OperationsApiDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        if (_database.ConnectionString is null)
        {
            return;
        }

        _api = await OperationsApiTestHost.StartAsync(_database.ConnectionString);
        _slug = "pkgs" + _tenant.ToString("N")[..6];
        _api.Platform.Tenant(_slug, _tenant, "active");
        await WithDatabaseAsync(context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OperationsTenantBrand brand = OperationsTenantBrand.Create(_tenant, "Parcel Express", now);
            brand.Publish(now);
            OperationsBranch branch = OperationsBranch.Create(_tenant, Details("MIA", "Miami"), OperationsBranchSide.Origin, now);
            OperationsWarehouse warehouse = OperationsWarehouse.Create(_tenant, branch.Id, Details("WH1", "Doral"), OperationsWarehouseRoles.Receiving, true, now);
            _warehouse = warehouse.Id;
            OperationsBranch pickup = OperationsBranch.Create(_tenant, new OperationsLocationDetails("PAP", "Port-au-Prince office",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            _branch = pickup.Id;
            context.AddRange(brand, branch, warehouse, pickup);
            return Task.CompletedTask;
        });

        HttpMessageHandler operations = _api.Handler();
        _portal = CustomerWebHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Customer.Web",
                "--AllowedHosts=*",
                "--ConnectionStrings:Customer=Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True",
                "--CustomerOperations:CallerKeyId=k1",
                "--CustomerOperations:CallerPrivateKey=" + _api.Customer.PrivateKey,
                "--CustomerBotCheck:Enabled=false",
                // Both hosts copy an appsettings.json into this bin folder; the portal's own values are given here so the copy order never matters.
                "--CustomerOperations:BaseUrl=https://" + OperationsApiTestHost.Host + "/",
                "--CustomerOperations:TimeoutSeconds=10",
                "--TenantHosting:BaseDomains:0=techcrafters.ltd"
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Services.AddHttpClient(CustomerOperationsApi.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => operations);
            });
        await _portal.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database.ConnectionString is null)
        {
            return;
        }

        await _portal.StopAsync();
        await _portal.DisposeAsync();
        await _api.DisposeAsync();
    }

    [SqlServerFact]
    public async Task Customer_CreatesShipments_AnnouncesPackages_AndSeesOnlyTheirOwn()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Assert.Contains("No shipment yet", await ana.GetStringAsync("/customer/packages"), StringComparison.Ordinal);
        Assert.Contains("Announce a package", await ana.GetStringAsync("/customer/packages/new"), StringComparison.Ordinal);

        // A shipment: where it goes, who receives it, the options and the first package (owner, 2026-10-18).
        string form = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/shipments/new"));
        Assert.Contains("Port-au-Prince office", form, StringComparison.Ordinal);
        Assert.Contains("value=\"Ana Pierre\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"line1\"", form, StringComparison.Ordinal);
        // Slice 5C (C-1): never without the number of parcels.
        Dictionary<string, string> uncounted = Shipment("1z999aa10123456784");
        uncounted.Remove("parcelCount");
        Assert.Contains("Enter how many parcels travel in this shipment, from 1 to 99.",
            WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", uncounted))), StringComparison.Ordinal);
        HttpResponseMessage created = await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("1z999aa10123456784"));
        string shipmentPath = created.Headers.Location!.OriginalString;
        Assert.Matches("^/customer/packages/shipments/[0-9a-f-]{36}\\?done=created$", shipmentPath);
        Guid shipment = Guid.Parse(shipmentPath["/customer/packages/shipments/".Length..^"?done=created".Length]);
        string page = WebUtility.HtmlDecode(await ana.GetStringAsync(shipmentPath));
        Assert.Contains("Port-au-Prince office", page, StringComparison.Ordinal);
        Assert.Contains("Ana Pierre · +50937123456", page, StringComparison.Ordinal);
        Assert.Contains("Consolidate", page, StringComparison.Ordinal);
        Assert.Contains("1Z999AA10123456784", page, StringComparison.Ordinal);

        // More packages in it; the same number twice is refused.
        string packages = "/customer/packages/shipments/" + shipment + "/packages";
        string twice = WebUtility.HtmlDecode(await Read(await ana.PostAsync(shipmentPath, packages, PreAlert("1Z999AA10123456784"))));
        Assert.Contains("This tracking number was already announced.", twice, StringComparison.Ordinal);
        Assert.Equal("/customer/packages/shipments/" + shipment + "?done=added", (await ana.PostAsync(shipmentPath, packages, PreAlert("TBA0009998887"))).Headers.Location?.OriginalString);

        // The instructions change while the shipment is open; home delivery is not offered by this company.
        Dictionary<string, string> changed = Instructions();
        changed["recipient"] = "Marie Pierre";
        HttpResponseMessage edited = await ana.PostAsync("/customer/packages/shipments/" + shipment + "/edit", "/customer/packages/shipments/" + shipment + "/edit", changed);
        Assert.Equal("/customer/packages/shipments/" + shipment + "?done=changed", edited.Headers.Location?.OriginalString);
        Assert.Contains("Marie Pierre", await ana.GetStringAsync("/customer/packages/shipments/" + shipment), StringComparison.Ordinal);

        // Slice 5C: the customer states how many boxes travel, from 1 to 99, and corrects it when another count arrives.
        changed["parcelCount"] = "0";
        Assert.Contains("Enter how many parcels travel in this shipment, from 1 to 99.",
            WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/" + shipment + "/edit", "/customer/packages/shipments/" + shipment + "/edit", changed))),
            StringComparison.Ordinal);
        changed["parcelCount"] = "3";
        Assert.Equal("/customer/packages/shipments/" + shipment + "?done=changed",
            (await ana.PostAsync("/customer/packages/shipments/" + shipment + "/edit", "/customer/packages/shipments/" + shipment + "/edit", changed)).Headers.Location?.OriginalString);
        Assert.Contains("0 of 3 received", WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/shipments/" + shipment)), StringComparison.Ordinal);
        Dictionary<string, string> home = Instructions();
        home["delivery"] = "home";
        Assert.Contains("Choose pickup at a branch or delivery to an address.",
            WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/" + shipment + "/edit", "/customer/packages/shipments/" + shipment + "/edit", home))), StringComparison.Ordinal);

        // A package withdrawn leaves the shipment.
        string list = await ana.GetStringAsync("/customer/packages/shipments/" + shipment);
        string withdrawPath = Regex.Match(list, "/customer/packages/pre-alerts/[0-9a-f-]{36}/withdraw").Value;
        Assert.Equal("/customer/packages?done=withdrawn", (await ana.PostAsync("/customer/packages", withdrawPath, new())).Headers.Location?.OriginalString);

        // A package that arrived before its announcement moves into the shipment and takes the declaration at once.
        (Guid parcel, Guid photo) = await ReceivedAsync(await CustomerIdAsync("ana@example.com"), "TBA0001112223");
        Assert.Contains("Packages waiting for a shipment", await ana.GetStringAsync("/customer/packages"), StringComparison.Ordinal);
        HttpResponseMessage matched = await ana.PostAsync("/customer/packages/shipments/" + shipment, packages, PreAlert("TBA0001112223"));
        Assert.Equal("/customer/packages/shipments/" + shipment + "?done=matched", matched.Headers.Location?.OriginalString);
        string withParcel = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/shipments/" + shipment));
        Assert.Contains("href=\"/customer/packages/" + parcel + "\"", withParcel, StringComparison.Ordinal);
        Assert.Contains("can no longer be cancelled", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/" + shipment,
            "/customer/packages/shipments/" + shipment + "/cancel", new()))), StringComparison.Ordinal);
        string parcelPage = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/" + parcel));
        Assert.Contains("A pair of running shoes", parcelPage, StringComparison.Ordinal);
        Assert.Contains("Received at our warehouse", parcelPage, StringComparison.Ordinal);
        Assert.Contains("You told us this package is on its way", parcelPage, StringComparison.Ordinal);
        Assert.Contains("No longer on hold", parcelPage, StringComparison.Ordinal);

        // No shelf, no staff step, no staff member on the customer page (ADR-0021 decision 5).
        Assert.DoesNotContain("A-01-1", parcelPage, StringComparison.Ordinal);
        Assert.DoesNotContain("condition", parcelPage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ui.tracking", parcelPage, StringComparison.Ordinal);

        HttpResponseMessage image = await ana.GetAsync("/customer/packages/" + parcel + "/photos/" + photo);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());

        // Another customer of the same company sees neither the shipment, the package nor its photo (S-3).
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync("/customer/packages/shipments/" + shipment)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync("/customer/packages/" + parcel)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync("/customer/packages/" + parcel + "/photos/" + photo)).StatusCode);
        Assert.DoesNotContain("TBA0001112223", await ben.GetStringAsync("/customer/packages"), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostAsync("/customer/packages", packages, PreAlert("TBA0004445556"))).StatusCode);

        // Ben may announce a number Ana announced: one customer's announcement never blocks another's (review H1).
        Assert.Matches("done=created$", (await ben.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("TBA0009998887"))).Headers.Location?.OriginalString);

        // A value written with a group separator is refused, never read as 1.2 (review M3).
        Dictionary<string, string> grouped = PreAlert("TBA0007776665");
        grouped["value"] = "1,200";
        Assert.Contains("Enter a value from 0 to 1,000,000.", WebUtility.HtmlDecode(await Read(await ana.PostAsync(shipmentPath, packages, grouped))), StringComparison.Ordinal);

        // A shipment none of whose packages arrived may be cancelled; its pre-alerts stay, in no shipment (Slice 4D).
        string spare = (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("TBA0005556667"))).Headers.Location!.OriginalString;
        Guid spareId = Guid.Parse(spare["/customer/packages/shipments/".Length..^"?done=created".Length]);
        Assert.Equal("/customer/packages?done=cancelled", (await ana.PostAsync(spare, "/customer/packages/shipments/" + spareId + "/cancel", new())).Headers.Location?.OriginalString);
        string afterCancel = await ana.GetStringAsync("/customer/packages");
        Assert.Contains("TBA0005556667", afterCancel, StringComparison.Ordinal);
        Assert.Contains("Not in a shipment yet", afterCancel, StringComparison.Ordinal);

        // Signed out: nothing.
        Assert.Equal(HttpStatusCode.Redirect, (await OpenBrowser().GetAsync("/customer/packages")).StatusCode);
    }

    // Slice 4D: an announcement in no shipment still counts (pre-alerts come first); one left in a cancelled shipment stays too; a package at another
    // warehouse goes only into a shipment from there.
    [SqlServerFact]
    public async Task AnnouncementsInNoShipment_StillCount_AndAPackageAtAnotherWarehouse_IsRefused()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        string first = (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("TBA0001230001"))).Headers.Location!.OriginalString;
        Assert.Matches("done=created$", first);
        string packages = first[..^"?done=created".Length] + "/packages";

        // Taken out of its shipment, the announcement still counts: the number cannot be announced twice.
        await WithDatabaseAsync(context => context.Set<OperationsPreAlert>().Where(item => item.TrackingNumber == "TBA0001230001")
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ShipmentId, (Guid?)null)));
        Assert.Contains("This tracking number was already announced.", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/new", "/customer/packages/new",
            PreAlert("TBA0001230001")))), StringComparison.Ordinal);
        await WithDatabaseAsync(async context =>
        {
            OperationsPreAlert announced = await context.Set<OperationsPreAlert>().SingleAsync(item => item.TrackingNumber == "TBA0001230001");
            Assert.Equal((OperationsPreAlertStatus.Open, _warehouse), (announced.Status, announced.WarehouseId));
        });

        // The package arrived at the company's other warehouse: never pulled into a shipment from this one.
        Guid other = Guid.Empty;
        await WithDatabaseAsync(async context =>
        {
            OperationsBranch branch = await context.Branches.FirstAsync(item => item.Side == OperationsBranchSide.Origin);
            OperationsWarehouse warehouse = OperationsWarehouse.Create(_tenant, branch.Id, Details("WH2", "Medley"), OperationsWarehouseRoles.Receiving, true, DateTimeOffset.UtcNow);
            other = warehouse.Id;
            context.Add(warehouse);
        });
        await ReceivedAsync(await CustomerIdAsync("ana@example.com"), "TBA0001230002", other);
        Assert.Contains("This package is at, or announced for, another of our warehouses.",
            WebUtility.HtmlDecode(await Read(await ana.PostAsync(first, packages, PreAlert("TBA0001230002")))), StringComparison.Ordinal);
    }

    // Slice 4D (owner, 2026-10-19): pre-alerts first, then shipments; several shipments at once, one without any package (goods dropped off);
    // a package moves between them, or out, and waits for its shipment when it arrives first.
    [SqlServerFact]
    public async Task Customer_AnnouncesFirst_ThenPutsPackagesIntoSeveralShipments_AndMovesThem()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Assert.Equal("/customer/packages?done=added", (await ana.PostAsync("/customer/packages/new", "/customer/packages/new", PreAlert("TBA0004440001"))).Headers.Location?.OriginalString);
        string listed = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages"));
        Assert.Contains("Not in a shipment yet", listed, StringComparison.Ordinal);
        string tape = Regex.Match(listed, "name=\"preAlert\" value=\"([0-9a-f-]{36})\"").Groups[1].Value;
        Assert.NotEmpty(tape);

        // The shipment to Peru takes the tape at once; the cartons' shipment has no package (dropped off later).
        Dictionary<string, string> peru = Instructions();
        peru["origin"] = _warehouse.ToString();
        peru["preAlerts"] = tape;
        string peruPath = (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", peru)).Headers.Location!.OriginalString;
        Assert.Matches("done=created$", peruPath);
        Assert.Contains("TBA0004440001", await ana.GetStringAsync(peruPath), StringComparison.Ordinal);
        Dictionary<string, string> cartons = Instructions();
        cartons["origin"] = _warehouse.ToString();
        cartons["recipient"] = "Carton buyer";
        string cartonsPath = (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", cartons)).Headers.Location!.OriginalString;
        Assert.Matches("done=created$", cartonsPath);
        Guid cartonsId = Guid.Parse(cartonsPath["/customer/packages/shipments/".Length..^"?done=created".Length]);

        // Moved to the other shipment, then out of both.
        Assert.Equal("/customer/packages?done=moved", (await ana.PostAsync("/customer/packages", "/customer/packages/move",
            new() { ["preAlert"] = tape, ["shipment"] = cartonsId.ToString(), ["back"] = "/customer/packages" })).Headers.Location?.OriginalString);
        Assert.Contains("TBA0004440001", await ana.GetStringAsync("/customer/packages/shipments/" + cartonsId), StringComparison.Ordinal);
        Assert.DoesNotContain("TBA0004440001", await ana.GetStringAsync(peruPath[..^"?done=created".Length]), StringComparison.Ordinal);
        Assert.Equal("/customer/packages/shipments/" + cartonsId + "?done=moved", (await ana.PostAsync("/customer/packages/shipments/" + cartonsId, "/customer/packages/move",
            new() { ["preAlert"] = tape, ["back"] = "/customer/packages/shipments/" + cartonsId })).Headers.Location?.OriginalString);
        await WithDatabaseAsync(async context => Assert.Null((await context.Set<OperationsPreAlert>().SingleAsync(item => item.Id == Guid.Parse(tape))).ShipmentId));

        // Another customer can neither move it nor see it; "back" never leaves the customer's pages.
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Contains("This announcement was not found.", WebUtility.HtmlDecode(await Read(await ben.PostAsync("/customer/packages", "/customer/packages/move",
            new() { ["preAlert"] = tape, ["back"] = "https://elsewhere.example" }))), StringComparison.Ordinal);

        // A package that arrives before its pre-alert is in a shipment waits for one; put in, the hold ends.
        (Guid parcel, Guid _) = await ReceivedAsync(await CustomerIdAsync("ana@example.com"), "TBA0004440002");
        Assert.Equal("/customer/packages?done=matched", (await ana.PostAsync("/customer/packages/new", "/customer/packages/new", PreAlert("TBA0004440002"))).Headers.Location?.OriginalString);
        await WithDatabaseAsync(async context => Assert.Equal(OperationsHoldReasons.AwaitingShipment, (await context.Parcels.SingleAsync(item => item.Id == parcel)).HoldReasons));
        Assert.Contains("Put it in one of your shipments", WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/" + parcel)), StringComparison.Ordinal);
        Assert.Contains("TBA0004440002", await ana.GetStringAsync("/customer/packages/shipments/" + cartonsId), StringComparison.Ordinal);
        Assert.Equal("/customer/packages/shipments/" + cartonsId + "?done=moved", (await ana.PostAsync("/customer/packages/shipments/" + cartonsId, "/customer/packages/move",
            new() { ["parcel"] = parcel.ToString(), ["shipment"] = cartonsId.ToString(), ["back"] = "/customer/packages/shipments/" + cartonsId })).Headers.Location?.OriginalString);
        await WithDatabaseAsync(async context =>
        {
            OperationsParcel moved = await context.Parcels.SingleAsync(item => item.Id == parcel);
            Assert.Equal((cartonsId, OperationsHoldReasons.None), (moved.ShipmentId!.Value, moved.HoldReasons));
            Assert.Equal(cartonsId, (await context.Set<OperationsPreAlert>().SingleAsync(item => item.ParcelId == parcel)).ShipmentId);
        });

        // Taken out again, it waits in no shipment with its hold.
        Assert.Equal("/customer/packages?done=moved", (await ana.PostAsync("/customer/packages", "/customer/packages/move",
            new() { ["parcel"] = parcel.ToString(), ["back"] = "/customer/packages" })).Headers.Location?.OriginalString);
        await WithDatabaseAsync(async context => Assert.Equal(OperationsHoldReasons.AwaitingShipment, (await context.Parcels.SingleAsync(item => item.Id == parcel)).HoldReasons));

        // The waiting shipment is never given instructions: its packages go into the customer's shipments (review of 4D).
        Guid waiting = Guid.Empty;
        await WithDatabaseAsync(async context => waiting = (await context.Parcels.SingleAsync(item => item.Id == parcel)).ShipmentId!.Value);
        Assert.Contains("These packages wait for a shipment", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/" + cartonsId + "/edit",
            "/customer/packages/shipments/" + waiting + "/edit", Instructions()))), StringComparison.Ordinal);
        Assert.DoesNotContain("/customer/packages/shipments/" + waiting + "/edit", await ana.GetStringAsync("/customer/packages/shipments/" + waiting), StringComparison.Ordinal);

        // A parcel and another pre-alert in one request are refused: a parcel moves only with its own pre-alert.
        Assert.Equal("/customer/packages?done=added", (await ana.PostAsync("/customer/packages/new", "/customer/packages/new", PreAlert("TBA0004440003"))).Headers.Location?.OriginalString);
        Guid other = Guid.Empty;
        await WithDatabaseAsync(async context => other = (await context.Set<OperationsPreAlert>().SingleAsync(item => item.TrackingNumber == "TBA0004440003")).Id);
        Assert.Contains("This package was not found", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages", "/customer/packages/move",
            new() { ["parcel"] = parcel.ToString(), ["preAlert"] = other.ToString(), ["shipment"] = cartonsId.ToString(), ["back"] = "/customer/packages" }))), StringComparison.Ordinal);

        // A parcel the customer already put in a shipment stays there when it is declared later on its own (review of 4D).
        (Guid undeclared, Guid _) = await ReceivedAsync(await CustomerIdAsync("ana@example.com"), "TBA0004440004");
        Assert.Equal("/customer/packages?done=moved", (await ana.PostAsync("/customer/packages", "/customer/packages/move",
            new() { ["parcel"] = undeclared.ToString(), ["shipment"] = cartonsId.ToString(), ["back"] = "/customer/packages" })).Headers.Location?.OriginalString);
        Assert.Equal("/customer/packages?done=matched", (await ana.PostAsync("/customer/packages/new", "/customer/packages/new", PreAlert("TBA0004440004"))).Headers.Location?.OriginalString);
        await WithDatabaseAsync(async context =>
        {
            OperationsParcel declared = await context.Parcels.SingleAsync(item => item.Id == undeclared);
            Assert.Equal((cartonsId, OperationsHoldReasons.None), (declared.ShipmentId!.Value, declared.HoldReasons));
            Assert.Equal(cartonsId, (await context.Set<OperationsPreAlert>().SingleAsync(item => item.TrackingNumber == "TBA0004440004")).ShipmentId);
        });
    }

    // A double click that gets past the browser guard: each customer form twice at the same moment never fails (ADR-0019).
    [SqlServerFact]
    public async Task EveryCustomerForm_SentTwiceAtOnce_NeverFails()
    {
        Browser browser = OpenBrowser();
        Dictionary<string, string> signUp = new()
        {
            ["firstName"] = "Lee",
            ["lastName"] = "Twice",
            ["email"] = "lee@example.com",
            ["phone"] = "+50937123499",
            ["language"] = "en",
            ["password"] = Password
        };
        List<string> failures = new();
        await TwiceAsync(failures, "sign-up", browser, "/customer/sign-up", "/customer/sign-up", signUp);
        string token = _api.Mailer.LastToken("lee@example.com");
        await TwiceAsync(failures, "verify", browser, "/customer/verify?token=" + Uri.EscapeDataString(token), "/customer/verify", new() { ["token"] = token });
        Browser again = OpenBrowser();
        await TwiceAsync(failures, "sign-in", again, "/customer/sign-in", "/customer/sign-in", new() { ["email"] = "lee@example.com", ["password"] = Password });
        await TwiceAsync(failures, "shipment", again, "/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("TBA0007776665"));
        Guid lee = await CustomerIdAsync("lee@example.com");
        await WithDatabaseAsync(async context => Assert.Equal(1, await context.Shipments.CountAsync(item => item.CustomerId == lee && item.DestinationCountryCode != null)));
        string shipment = Regex.Match(await again.GetStringAsync("/customer/packages"), "/customer/packages/shipments/[0-9a-f-]{36}").Value;
        await TwiceAsync(failures, "pre-alert", again, shipment, shipment + "/packages", PreAlert("TBA0007776666"));
        await TwiceAsync(failures, "change", again, shipment + "/edit", shipment + "/edit", Instructions());
        string list = await again.GetStringAsync(shipment);
        string withdrawPath = Regex.Match(list, "/customer/packages/pre-alerts/[0-9a-f-]{36}/withdraw").Value;
        Assert.NotEmpty(withdrawPath);
        await TwiceAsync(failures, "withdraw", again, "/customer/packages", withdrawPath, new());

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    // A double click: the same form, from the one page, sent twice at the same moment.
    private static async Task TwiceAsync(List<string> failures, string name, Browser browser, string pagePath, string postPath, Dictionary<string, string> form)
    {
        string page = await browser.GetStringAsync(pagePath);

        foreach (HttpResponseMessage response in await Task.WhenAll(browser.PostFromAsync(page, postPath, form), browser.PostFromAsync(page, postPath, form)))
        {
            if ((int)response.StatusCode >= 400)
            {
                string body = await response.Content.ReadAsStringAsync();
                failures.Add(name + ": " + (int)response.StatusCode + " " + (body.Length > 300 ? body[..300] : body));
            }
        }
    }

    // Pickup at the company's Port-au-Prince branch, received by the customer, consolidated.
    private Dictionary<string, string> Instructions() => new()
    {
        ["delivery"] = "pickup",
        ["branch"] = _branch.ToString(),
        ["recipient"] = "Ana Pierre",
        ["recipientPhone"] = "+50937123456",
        ["parcelCount"] = "5",
        ["consolidate"] = "true"
    };

    private Dictionary<string, string> Shipment(string tracking)
    {
        Dictionary<string, string> form = Instructions();

        foreach (KeyValuePair<string, string> field in PreAlert(tracking))
        {
            form[field.Key] = field.Value;
        }

        form["origin"] = _warehouse.ToString();
        return form;
    }

    private static Dictionary<string, string> PreAlert(string tracking) => new()
    {
        ["carrier"] = "Amazon",
        ["tracking"] = tracking,
        ["store"] = "Runner shop",
        ["contents"] = "A pair of running shoes",
        ["value"] = "89.90"
    };

    // A package as receiving leaves it: in the customer's open shipment, photographed, held for information, on a shelf.
    private async Task<(Guid Parcel, Guid Photo)> ReceivedAsync(Guid customerId, string tracking, Guid? at = null)
    {
        Guid warehouse = at ?? _warehouse;
        await using AsyncServiceScope scope = _api.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(_tenant);
        IServiceProvider services = scope.ServiceProvider;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(_tenant, warehouse, "Amazon", Guid.NewGuid(), Guid.NewGuid().ToString("N"), now);
        services.GetRequiredService<IOperationsReceivingRepository>().Add(delivery);
        OperationsParcel parcel = OperationsParcel.Receive(_tenant, "PK26" + Random.Shared.Next(100000, 999999), delivery, "Amazon", tracking, false, now);
        parcel.Measure(OperationsMeasurements.Create(1.2m, 30, 20, 10), now);
        IOperationsReceivingRepository receiving = services.GetRequiredService<IOperationsReceivingRepository>();

        // The customer's one waiting shipment at the warehouse, as the dock uses it.
        OperationsShipment? shipment = await receiving.FindOpenShipmentAsync(customerId, warehouse, CancellationToken.None);

        if (shipment is null)
        {
            shipment = OperationsShipment.Open(_tenant, customerId, warehouse, now);
            receiving.Add(shipment);
        }

        parcel.JoinShipment(shipment, now);
        services.GetRequiredService<IOperationsTrackingRepository>().Add(parcel);
        string key = await services.GetRequiredService<IOperationsFileStore>().SaveAsync(_tenant, "parcels", Png(), "image/png", CancellationToken.None);
        OperationsParcelPhoto photo = OperationsParcelPhoto.Add(_tenant, parcel.Id, "label", key, "image/png", 64, new string('a', 64), Guid.NewGuid(), now);
        receiving.Add(photo);

        OperationsTracker tracker = services.GetRequiredService<OperationsTracker>();
        Guid clerk = Guid.NewGuid();
        string scan = "seed" + Guid.NewGuid().ToString("N");
        foreach ((string code, string name, string value) in new[]
        {
            (OperationsTrackingCodes.ReceivedAtOrigin, "warehouse", "WH1"),
            (OperationsTrackingCodes.ConditionRecorded, "condition", "intact"),
            (OperationsTrackingCodes.HoldOpened, "reason", nameof(OperationsHoldReasons.AwaitingCustomerInformation)),
            (OperationsTrackingCodes.PutAway, "location", "A-01-1")
        })
        {
            OperationsTrackingResult result = await tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, code,
                new Dictionary<string, string>(StringComparer.Ordinal) { [name] = value }, now, scan + ":" + code, OperationsTrackingActor.Staff, OperationsTrackingSource.Scan)
            {
                ActorUserId = clerk,
                LocationCode = "WH1"
            }, CancellationToken.None);
            Assert.Null(result.Error);
        }

        Assert.True(await services.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));
        return (parcel.Id, photo.Id);
    }

    // Slice 3E: the customer answers a hold on the package page; a file that is not a PDF, PNG or JPEG is refused.
    [SqlServerFact]
    public async Task Customer_AnswersAHold_WithADocument()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        (Guid parcel, Guid _) = await ReceivedAsync(await CustomerIdAsync("ana@example.com"), "TBA0007778881");
        await WithDatabaseAsync(context => context.Parcels.Where(item => item.Id == parcel)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.HoldReasons, OperationsHoldReasons.InspectionValue)));
        string page = "/customer/packages/" + parcel;
        string form = WebUtility.HtmlDecode(await ana.GetStringAsync(page));
        Assert.Contains("action=\"/customer/packages/" + parcel + "/answers\"", form, StringComparison.Ordinal);
        Assert.Contains("Send my answer", form, StringComparison.Ordinal);
        Dictionary<string, string> fields = new(StringComparer.Ordinal) { ["reason"] = "InspectionValue", ["contents"] = "Two used phones", ["value"] = "300.00" };

        HttpResponseMessage refused = await ana.PostFileAsync(page, page + "/answers", fields, "invoice.txt", "plain text"u8.ToArray(), "text/plain");
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Contains("The file must be a PDF, PNG or JPEG of up to 10 MB.", WebUtility.HtmlDecode(await Read(refused)), StringComparison.Ordinal);

        HttpResponseMessage sent = await ana.PostFileAsync(page, page + "/answers", fields, "invoice.png", Png(), "image/png");
        Assert.Equal(page + "?done=answered", sent.Headers.Location?.OriginalString);
        Assert.Contains("We received your answer. Our team is reviewing it.", WebUtility.HtmlDecode(await ana.GetStringAsync(page)), StringComparison.Ordinal);

        // Another customer cannot answer for this package.
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostFileAsync("/customer/packages", page + "/answers", fields, "invoice.png", Png(), "image/png")).StatusCode);
    }

    // Slice 5F (I-8): the customer sees their own invoices and approved credit notes with their PDFs, never another customer's.
    [SqlServerFact]
    public async Task Customer_SeesTheirOwnInvoices_AndCreditNotes_WithThePdfs()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Guid anaId = await CustomerIdAsync("ana@example.com");
        Guid invoiceId = Guid.Empty;
        Guid creditId = Guid.Empty;

        await WithDatabaseAsync(context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Vixly.Operations.Domain.Billing.OperationsInvoiceLine[] lines =
            {
                new(null, Guid.NewGuid(), "TAPE", "Packing tape", null, 2m, null, 4.50m, 9.00m, 0m, 0m, Vixly.Operations.Domain.Billing.OperationsTaxKind.Supplies, true)
            };
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = Vixly.Operations.Domain.Billing.OperationsInvoice.Issue(_tenant, "INV26000777",
                new Vixly.Operations.Domain.Billing.OperationsInvoiceDraft(Vixly.Operations.Domain.Billing.OperationsInvoiceKind.SupplyOrder, anaId, _warehouse, null, null, "SO26000777", null, null,
                    "USD", "en", new Vixly.Operations.Domain.Billing.OperationsInvoiceParty("Ana's carrier", null, "Miami", null, "Ana", null, "+1", "ana@example.com"), lines, "Sales tax", false,
                    new Vixly.Operations.Domain.Billing.OperationsTaxRates(0m, 0m, 7m, 0m), null), DateOnly.FromDateTime(now.UtcDateTime), now);
            Vixly.Operations.Domain.Billing.OperationsCreditNote credit = Vixly.Operations.Domain.Billing.OperationsCreditNote.RequestCredit(_tenant, invoice,
                new Dictionary<Guid, decimal> { [invoice.Lines[0].Id] = 4.50m }, "Damaged", Guid.NewGuid(), Guid.NewGuid(), now);
            credit.Approve(invoice, "CN26000777", Guid.NewGuid(), DateOnly.FromDateTime(now.UtcDateTime), now);

            // Slice 5F step 2b: billed to Ana's credit account, due in 30 days.
            invoice.ChargeToAccount(true, DateOnly.FromDateTime(now.UtcDateTime).AddDays(30), Guid.NewGuid(), now);
            context.Add(Vixly.Operations.Domain.Billing.OperationsCreditAccount.Open(_tenant, anaId, 500m, null, Guid.NewGuid(), now));
            context.AddRange(invoice, credit);
            (invoiceId, creditId) = (invoice.Id, credit.Id);
            return Task.CompletedTask;
        });

        string list = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/invoices"));
        Assert.Contains("INV26000777", list, StringComparison.Ordinal);
        Assert.Contains("CN26000777", list, StringComparison.Ordinal);
        Assert.Contains("SO26000777", list, StringComparison.Ordinal);
        HttpResponseMessage pdf = await ana.GetAsync("/customer/invoices/" + invoiceId + "/pdf");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/pdf", (await ana.GetAsync("/customer/invoices/" + invoiceId + "/pdf?credit=" + creditId)).Content.Headers.ContentType?.MediaType);
        Assert.Contains("Your credit account", list, StringComparison.Ordinal);
        Assert.Contains("On your account", list, StringComparison.Ordinal);
        Assert.Contains("pay within 30 days", list, StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await ana.GetAsync("/customer/invoices/statement")).Content.Headers.ContentType?.MediaType);

        // Another customer never sees it nor prints it.
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.DoesNotContain("INV26000777", await ben.GetStringAsync("/customer/invoices"), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync("/customer/invoices/" + invoiceId + "/pdf")).StatusCode);
        Assert.DoesNotContain("Your credit account", WebUtility.HtmlDecode(await ben.GetStringAsync("/customer/invoices")), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync("/customer/invoices/statement")).StatusCode);
    }

    // Slice 5F step 4: a customer pays their own invoice by card on Stripe's page, recorded on Stripe's word when they come back; never another customer's invoice.
    [SqlServerFact]
    public async Task Customer_PaysTheirInvoiceByCard_OnStripesPage_NeverAnothersInvoice()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Guid anaId = await CustomerIdAsync("ana@example.com");
        Guid invoiceId = Guid.Empty;

        await WithDatabaseAsync(async context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Vixly.Operations.Domain.Billing.OperationsInvoiceLine[] lines =
            {
                new(null, Guid.NewGuid(), "TAPE", "Packing tape", null, 2m, null, 4.50m, 9.00m, 0m, 0m, Vixly.Operations.Domain.Billing.OperationsTaxKind.Supplies, true)
            };
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = Vixly.Operations.Domain.Billing.OperationsInvoice.Issue(_tenant, "INV26000888",
                new Vixly.Operations.Domain.Billing.OperationsInvoiceDraft(Vixly.Operations.Domain.Billing.OperationsInvoiceKind.SupplyOrder, anaId, _warehouse, null, null, "SO26000888", null, null,
                    "USD", "en", new Vixly.Operations.Domain.Billing.OperationsInvoiceParty("Ana's carrier", null, "Miami", null, "Ana", null, "+1", "ana@example.com"), lines, "Sales tax", false,
                    new Vixly.Operations.Domain.Billing.OperationsTaxRates(0m, 0m, 0m, 0m), null), DateOnly.FromDateTime(now.UtcDateTime), now);
            IOperationsSecretProtector protector = _api.Services.GetRequiredService<IOperationsSecretProtector>();
            context.Add(Vixly.Operations.Domain.Billing.OperationsCardProvider.Connect(_tenant, "sk_test_portal1234567890", protector.Protect("sk_test_portal1234567890"),
                "pk_test_portal1234567890", protector.Protect("whsec_portal1234567890abcd"), "Test", Guid.NewGuid(), now));
            Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await context.TenantSettings.SingleOrDefaultAsync();

            if (settings is null)
            {
                settings = Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(_tenant, now);
                context.Add(settings);
            }

            settings.ChangeCard(true, now);
            context.Add(invoice);
            invoiceId = invoice.Id;
        });

        string list = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/invoices"));
        Assert.Contains("action=\"/customer/invoices/" + invoiceId + "/card\"", list, StringComparison.Ordinal);

        // Another customer cannot start one on Ana's invoice.
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Contains("This invoice was not found.", WebUtility.HtmlDecode(await Read(await ben.PostAsync("/customer/invoices", "/customer/invoices/" + invoiceId + "/card", new()))),
            StringComparison.Ordinal);

        // Ana: on to Stripe's page, back here; paid only once Stripe says so.
        HttpResponseMessage started = await ana.PostAsync("/customer/invoices", "/customer/invoices/" + invoiceId + "/card", new());
        string chargePath = started.Headers.Location!.OriginalString;
        Assert.Matches("^/customer/invoices/card/[0-9a-f-]{36}$", chargePath);
        string session = "cs_test_" + Guid.Parse(chargePath[^36..]).ToString("N");
        Assert.EndsWith("/customer/invoices/card/" + chargePath[^36..], _api.Cards.Asked[session].ReturnUrl, StringComparison.Ordinal);
        Assert.Equal(9.00m, _api.Cards.Asked[session].Amount);
        string waiting = WebUtility.HtmlDecode(await ana.GetStringAsync(chargePath));
        Assert.Contains("https://checkout.stripe.com/c/pay/" + session, waiting, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync(chargePath)).StatusCode);

        _api.Cards.Pay(session);
        Assert.Contains("Thank you: your payment is confirmed.", WebUtility.HtmlDecode(await ana.GetStringAsync(chargePath)), StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"/customer/invoices/" + invoiceId + "/card\"", await ana.GetStringAsync("/customer/invoices"), StringComparison.Ordinal);

        await WithDatabaseAsync(async context =>
        {
            Assert.Equal(0m, (await context.Invoices.AsNoTracking().SingleAsync(item => item.Id == invoiceId)).Owed);
            Vixly.Operations.Domain.Billing.OperationsPayment payment = await context.Payments.AsNoTracking().SingleAsync(item => item.InvoiceId == invoiceId);
            Assert.Null(payment.TakenByUserId);
        });
    }

    // Slice 4D step 4: a customer reports a package the carrier delivered; staff find it and the customer sees it, never another customer.
    [SqlServerFact]
    public async Task Customer_ReportsAMissingPackage_SeesTheAnswer_AndOnlyTheirOwnTickets()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Assert.Contains("Report a missing package", WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/tickets")), StringComparison.Ordinal);
        Dictionary<string, string> fields = new(StringComparer.Ordinal)
        {
            ["carrier"] = "Amazon", ["tracking"] = "TBA 5551 2223 33", ["store"] = "Amazon", ["order"] = "113-4445556", ["delivered"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["contents"] = "Packing tape", ["message"] = "Left at the front desk."
        };

        HttpResponseMessage refused = await ana.PostFileAsync("/customer/tickets/new", "/customer/tickets/new", fields, "proof.txt", "plain text"u8.ToArray(), "text/plain", "proof");
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Contains("The proof of delivery must be a PDF, PNG or JPEG file of up to 10 MB.", WebUtility.HtmlDecode(await Read(refused)), StringComparison.Ordinal);

        HttpResponseMessage opened = await ana.PostFileAsync("/customer/tickets/new", "/customer/tickets/new", fields, "proof.png", Png(), "image/png", "proof");
        Assert.Equal(HttpStatusCode.Redirect, opened.StatusCode);
        string page = opened.Headers.Location!.OriginalString.Split('?')[0];
        Guid ticketId = Guid.Parse(page["/customer/tickets/".Length..]);
        string shown = WebUtility.HtmlDecode(await ana.GetStringAsync(page + "?done=opened"));
        Assert.Contains("Our team will look for your package", shown, StringComparison.Ordinal);
        Assert.Contains("TBA5551222333", shown, StringComparison.Ordinal);
        Assert.Contains("Left at the front desk.", shown, StringComparison.Ordinal);
        Assert.Contains("We are looking", shown, StringComparison.Ordinal);

        // Another customer never sees it, nor writes on it.
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync(page)).StatusCode);
        Assert.DoesNotContain("TBA5551222333", await ben.GetStringAsync("/customer/tickets"), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostAsync("/customer/tickets", page + "/messages", new() { ["message"] = "Mine?", ["key"] = Guid.NewGuid().ToString("N") })).StatusCode);

        // The same report sent again opens the first one, never a second ticket.
        HttpResponseMessage again = await ana.PostFileAsync("/customer/tickets/new", "/customer/tickets/new", fields, "proof.png", Png(), "image/png", "proof");
        Assert.Equal(page, again.Headers.Location!.OriginalString.Split('?')[0]);

        // Staff found it and gave it to Ana: she sees the link and the answer, the proof was kept with a checksum.
        Guid anaId = await CustomerIdAsync("ana@example.com");
        (Guid parcel, Guid _) = await ReceivedAsync(anaId, "TBA5551222333");
        await WithDatabaseAsync(async context =>
        {
            Vixly.Operations.Domain.Tickets.OperationsTicket ticket = await context.Tickets.SingleAsync(item => item.Id == ticketId);
            Assert.Equal(("image/png", 64), (ticket.ProofContentType, ticket.ProofSha256!.Length));
            ticket.Resolve(Guid.NewGuid(), parcel, "Found it on the undeclared shelf.", DateTimeOffset.UtcNow);
        });
        shown = WebUtility.HtmlDecode(await ana.GetStringAsync(page));
        Assert.Contains("We found your package.", shown, StringComparison.Ordinal);
        Assert.Contains("/customer/packages/" + parcel, shown, StringComparison.Ordinal);
        Assert.Contains("Found it on the undeclared shelf.", shown, StringComparison.Ordinal);

        // Writing back opens it again for staff; the same form sent twice writes once.
        string key = Guid.NewGuid().ToString("N");
        HttpResponseMessage replied = await ana.PostAsync(page, page + "/messages", new() { ["message"] = "Thank you, one more is missing.", ["key"] = key });
        Assert.Equal(page + "?done=sent", replied.Headers.Location?.OriginalString);
        Assert.Equal(page + "?done=sent", (await ana.PostAsync(page, page + "/messages", new() { ["message"] = "Thank you, one more is missing.", ["key"] = key })).Headers.Location?.OriginalString);
        string reopened = WebUtility.HtmlDecode(await ana.GetStringAsync(page));
        Assert.Contains("We are looking", reopened, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(reopened, "Thank you, one more is missing."));
    }

    // Slice 5B: the customer reads what the company owes when not insuring, insures, and downloads the certificate; another customer never.
    [SqlServerFact]
    public async Task Customer_SeesTheLiabilityTerms_InsuresAShipment_AndDownloadsItsCertificate()
    {
        await WithDatabaseAsync(context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow.AddDays(-1);
            context.Add(Vixly.Operations.Domain.Charges.OperationsInsurancePlan.Create(_tenant, Vixly.Operations.Domain.Charges.OperationsTransportMode.Air,
                new Vixly.Operations.Domain.Charges.OperationsInsuranceTerms(Vixly.Operations.Domain.Charges.OperationsInsuranceMethod.Percent, 3m, 5m,
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsuranceBand>(), 5000m), null, now));
            Vixly.Operations.Domain.Insurance.OperationsLiabilityTerms terms = Vixly.Operations.Domain.Insurance.OperationsLiabilityTerms.Create(_tenant,
                Vixly.Operations.Domain.Charges.OperationsTransportMode.Air, now);
            terms.Change(Vixly.Operations.Domain.Insurance.OperationsLiabilityBasis.PerParcel, 50m, Vixly.Operations.Domain.Settings.OperationsWeightUnit.Pound, null, null, now);
            context.Add(terms);
            return Task.CompletedTask;
        });

        Browser ana = await ConfirmedAsync("ana@example.com");
        string form = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/shipments/new"));
        Assert.Contains("By air, if not insured: we pay up to 50 USD per parcel lost or damaged.", form, StringComparison.Ordinal);

        Dictionary<string, string> fields = Shipment("1z999aa10123456711");
        (fields["insured"], fields["insuranceAccepted"]) = ("true", "true");
        HttpResponseMessage created = await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", fields);
        string shipmentPath = created.Headers.Location!.OriginalString.Split('?')[0];
        Assert.Contains("/certificate", await ana.GetStringAsync(shipmentPath), StringComparison.Ordinal);

        HttpResponseMessage pdf = await ana.GetAsync(shipmentPath + "/certificate");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", await pdf.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync(shipmentPath + "/certificate")).StatusCode);

        // Review: the company stopping the cover later never voids a certificate already issued; reading it never issues another.
        Guid shipmentId = Guid.Parse(shipmentPath["/customer/packages/shipments/".Length..]);
        await WithDatabaseAsync(async context => (await context.InsurancePlans.SingleAsync()).ChangeStatus(false, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(shipmentPath + "/certificate")).StatusCode);
        List<Vixly.Operations.Domain.Insurance.OperationsCertificateStatus> statuses = new();
        await WithDatabaseAsync(async context => statuses = await context.InsuranceCertificates.Where(item => item.ShipmentId == shipmentId).Select(item => item.Status).ToListAsync());
        Assert.Equal(new[] { Vixly.Operations.Domain.Insurance.OperationsCertificateStatus.Provisional }, statuses);

        // Cancelled: the certificate is voided and still opens, marked void.
        await ana.PostAsync(shipmentPath, shipmentPath + "/cancel", new());
        await WithDatabaseAsync(async context => statuses = await context.InsuranceCertificates.Where(item => item.ShipmentId == shipmentId).Select(item => item.Status).ToListAsync());
        Assert.Equal(new[] { Vixly.Operations.Domain.Insurance.OperationsCertificateStatus.Void }, statuses);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(shipmentPath + "/certificate")).StatusCode);
    }

    // Slice 5A: the customer downloads the packing list and the pro forma invoice of its own shipment; another customer never.
    [SqlServerFact]
    public async Task Customer_DownloadsTheDocumentsOfTheirShipment_NeverAnothers()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        HttpResponseMessage created = await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("1z999aa10123456700"));
        string shipmentPath = created.Headers.Location!.OriginalString.Split('?')[0];
        Guid shipmentId = Guid.Parse(shipmentPath["/customer/packages/shipments/".Length..]);
        Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync(shipmentPath + "/documents/packing-list")).StatusCode);
        Assert.DoesNotContain("/documents/packing-list", await ana.GetStringAsync(shipmentPath), StringComparison.Ordinal);

        (Guid parcel, Guid _) = await ReceivedAsync(await CustomerIdAsync("ana@example.com"), "1Z999AA10123456700");
        await WithDatabaseAsync(async context =>
        {
            OperationsShipment shipment = await context.Shipments.SingleAsync(item => item.Id == shipmentId);
            (await context.Parcels.SingleAsync(item => item.Id == parcel)).JoinShipment(shipment, DateTimeOffset.UtcNow);
        });

        Assert.Contains("/documents/commercial-invoice", await ana.GetStringAsync(shipmentPath), StringComparison.Ordinal);

        foreach (string kind in new[] { "packing-list", "commercial-invoice" })
        {
            HttpResponseMessage pdf = await ana.GetAsync(shipmentPath + "/documents/" + kind);
            Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.StartsWith("%PDF", await pdf.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync(shipmentPath + "/documents/anything")).StatusCode);
        Browser ben = await ConfirmedAsync("ben@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync(shipmentPath + "/documents/packing-list")).StatusCode);
    }

    // Slice 4: the calculator uses the company's prices, or the customer's own card, never another customer's; quotes and shipment estimates.
    [SqlServerFact]
    public async Task Customer_ChoosesTheDestination_PickupOrHomeInItsArea_AndAcceptsTheInsuranceTerms()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Guid capital = Guid.Empty;
        Guid jeremie = Guid.Empty;
        await WithDatabaseAsync(context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OperationsWarehouse west = OperationsWarehouse.Create(_tenant, _branch, Place("PAP", "Port-au-Prince", "Port-au-Prince"), OperationsWarehouseRoles.PickupPoint, false, now);
            OperationsWarehouse grandAnse = OperationsWarehouse.Create(_tenant, _branch, Place("JER", "Jérémie", "Jérémie"), OperationsWarehouseRoles.PickupPoint, false, now);
            grandAnse.ChangeDestination(new OperationsDestinationTerms(true, null, OperationsDestinationOperator.Partner, "Grand'Anse Express", null, null, new[] { "Jérémie" }), now);
            Vixly.Operations.Domain.Charges.OperationsInsurancePlan plan = Vixly.Operations.Domain.Charges.OperationsInsurancePlan.Create(_tenant,
                Vixly.Operations.Domain.Charges.OperationsTransportMode.Air, new Vixly.Operations.Domain.Charges.OperationsInsuranceTerms(Vixly.Operations.Domain.Charges.OperationsInsuranceMethod.Percent,
                    3m, 5m, Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsuranceBand>(), null, Vixly.Operations.Domain.Charges.OperationsInsuranceCoverage.Defaults with
                    {
                        Cover = Vixly.Operations.Domain.Charges.OperationsInsuranceCover.Insurer, InsurerName = "Caribbean Cargo Insurance", PolicyNumber = "CC-2026"
                    }), null, now);
            (capital, jeremie) = (west.Id, grandAnse.Id);
            context.AddRange(west, grandAnse, plan);
            return Task.CompletedTask;
        });

        string form = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/packages/shipments/new"));
        Assert.Contains("Deliver to", form, StringComparison.Ordinal);
        Assert.Contains("<option value=\"" + jeremie + "\"", form, StringComparison.Ordinal);
        // Slice 5D: the product, by its name, to choose; a company with one product needs no choice beyond insuring.
        Assert.Contains("<strong>Air insurance</strong>", form, StringComparison.Ordinal);
        Assert.Contains("name=\"insurance\"", form, StringComparison.Ordinal);
        Assert.Contains("Insured with Caribbean Cargo Insurance (policy CC-2026).", form, StringComparison.Ordinal);
        Assert.Contains("Cash and negotiable documents", form, StringComparison.Ordinal);
        Assert.Contains("I accept the insurance terms", form, StringComparison.Ordinal);

        // Pickup in Jérémie; home delivery refused here (the company offers pickup only; the area rule is proven in the engine tests).
        Dictionary<string, string> pickup = Shipment("TBA0007776661");
        pickup["destination"] = jeremie.ToString();
        Assert.Equal(HttpStatusCode.Redirect, (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", pickup)).StatusCode);
        Dictionary<string, string> home = Shipment("TBA0007776662");
        (home["destination"], home["delivery"], home["line1"], home["city"], home["country"]) = (jeremie.ToString(), "home", "5 Rue", "Les Cayes", "HT");
        Assert.Contains("This delivery is not offered there.", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", home))),
            StringComparison.Ordinal);

        // Insured only with the terms accepted, and the acceptance is recorded.
        Dictionary<string, string> insured = Shipment("TBA0007776663");
        (insured["destination"], insured["insured"]) = (capital.ToString(), "true");
        Assert.Contains("Accept the insurance terms to insure the shipment.", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/new",
            "/customer/packages/shipments/new", insured))), StringComparison.Ordinal);
        insured["insuranceAccepted"] = "true";
        Assert.Equal(HttpStatusCode.Redirect, (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", insured)).StatusCode);

        List<(Guid?, DateTimeOffset?)> shipments = new();
        await WithDatabaseAsync(async context => shipments = (await context.Shipments.OrderBy(item => item.CreatedUtc).ToListAsync())
            .Select(item => (item.DestinationWarehouseId, item.InsuranceAcceptedUtc)).ToList());
        Assert.Equal(new Guid?[] { jeremie, capital }, shipments.Select(item => item.Item1).ToArray());
        Assert.Null(shipments[0].Item2);
        Assert.NotNull(shipments[1].Item2);
    }

    private static OperationsLocationDetails Place(string code, string name, string city) =>
        new(code, name, OperationsPostalAddress.Create("1 Rue", null, city, null, null, "HT"), null, null, null, false);

    [SqlServerFact]
    public async Task Customer_EstimatesRates_SeesTheirQuotes_AndOnlyTheirOwnPrices()
    {
        Browser ana = await ConfirmedAsync("ana@example.com");
        Browser ben = await ConfirmedAsync("ben@example.com");
        Guid anaId = await CustomerIdAsync("ana@example.com");
        Guid benId = await CustomerIdAsync("ben@example.com");
        Guid destination = Guid.Empty;
        await WithDatabaseAsync(context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup air = Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup.Create(_tenant, "air_standard", now);
            air.Change(true, null, 4, 7, 6000m, true, now);
            Vixly.Operations.Domain.Charges.OperationsRatePrices Prices(decimal price) =>
                new(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, price) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null);
            OperationsWarehouse haiti = OperationsWarehouse.Create(_tenant, _branch, Place("HTH", "Haiti home delivery", "Port-au-Prince"), OperationsWarehouseRoles.Storage, false, now);
            haiti.ChangeDestination(new OperationsDestinationTerms(true, null, OperationsDestinationOperator.Company, null, null, null, null), now);
            destination = haiti.Id;
            Vixly.Operations.Domain.Charges.OperationsQuote quote = Vixly.Operations.Domain.Charges.OperationsQuote.Create(_tenant, "QT26000042", anaId, "air_standard", "HT", false, false,
                new[] { new Vixly.Operations.Domain.Charges.OperationsQuotePiece(5m, 0, 0, 0, 0, null, null) },
                new[] { new Vixly.Operations.Domain.Charges.OperationsQuoteLine(Vixly.Operations.Domain.Charges.OperationsChargeLineKind.Freight, null, 1, 5, 5, 2.5m, 12.5m, false) },
                "USD", 10m, "Welcome price", 14, Guid.NewGuid(), now);
            context.AddRange(air, haiti, Vixly.Operations.Domain.Charges.OperationsRateCard.Create(_tenant, "air_standard", null, null, null,
                    Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly, Prices(2.50m), null, now),
                Vixly.Operations.Domain.Charges.OperationsRateCard.Create(_tenant, "air_standard", null, haiti.Id, benId, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                    Prices(0.50m), null, now),
                quote);
            return Task.CompletedTask;
        });

        string page = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/rates"));
        Assert.Contains("Estimate: the final amount is set when your packages are measured", page, StringComparison.Ordinal);
        Assert.Contains("<option value=\"" + destination + "\"", page, StringComparison.Ordinal);
        Assert.Contains("QT26000042", page, StringComparison.Ordinal);
        Assert.Contains("Your preferred price", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Welcome price", page, StringComparison.Ordinal);
        Assert.Contains("href=\"/customer/rates\"", page, StringComparison.Ordinal);

        Dictionary<string, string> form = new(StringComparer.Ordinal) { ["destination"] = destination.ToString(), ["weight"] = "3", ["value"] = "50" };
        string estimate = WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/rates", "/customer/rates", form)));
        Assert.Contains("Air standard · estimate", estimate, StringComparison.Ordinal);
        Assert.Contains("7.50 USD", estimate, StringComparison.Ordinal);
        Assert.Contains("1.50 USD", WebUtility.HtmlDecode(await Read(await ben.PostAsync("/customer/rates", "/customer/rates", form))), StringComparison.Ordinal);
        Assert.DoesNotContain("QT26000042", await ben.GetStringAsync("/customer/rates"), StringComparison.Ordinal);

        string refused = WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/rates", "/customer/rates", new Dictionary<string, string>(form) { ["weight"] = "1,5" })));
        Assert.Contains("Enter a weight above zero", refused, StringComparison.Ordinal);
        Assert.Contains("Choose where it ships from and to from the list.", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/rates", "/customer/rates",
            new Dictionary<string, string>(form) { ["destination"] = Guid.NewGuid().ToString() }))), StringComparison.Ordinal);

        // The Pricing page (owner, 2026-10-21): the regular prices for everyone, a customer's own prices only to that customer, first.
        string pricing = WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/pricing"));
        Assert.Contains("href=\"/customer/pricing\"", pricing, StringComparison.Ordinal);
        Assert.Contains("from 0 kg: 2.50 USD / kg", pricing, StringComparison.Ordinal);
        Assert.DoesNotContain("0.50 USD", pricing, StringComparison.Ordinal);
        Assert.DoesNotContain("Your prices", pricing, StringComparison.Ordinal);
        string benPricing = WebUtility.HtmlDecode(await ben.GetStringAsync("/customer/pricing"));
        Assert.True(benPricing.IndexOf("0.50 USD / kg", StringComparison.Ordinal) is > 0 and var own && own < benPricing.IndexOf("2.50 USD / kg", StringComparison.Ordinal));
        Assert.Contains("Your prices", benPricing, StringComparison.Ordinal);
        Assert.Contains("Haiti home delivery", benPricing, StringComparison.Ordinal);

        // Slice 5D: the shipment estimate beside the shipment form: the service's rates while no package is in, the customer's own prices, and nothing without a destination.
        Dictionary<string, string> building = new(StringComparer.Ordinal)
        {
            ["destination"] = destination.ToString(), ["delivery"] = "home", ["service"] = "air_standard", ["recipient"] = "Ana Pierre", ["recipientPhone"] = "+50937123456"
        };
        string panel = WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/estimate", building)));
        Assert.Contains("Shipment estimate", panel, StringComparison.Ordinal);
        Assert.Contains("from 0: 2.50 USD / kg", panel, StringComparison.Ordinal);
        Assert.Contains("from 0: 0.50 USD / kg", WebUtility.HtmlDecode(await Read(await ben.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/estimate", building))),
            StringComparison.Ordinal);
        Assert.Contains("Choose the destination and the service to see the estimate.", WebUtility.HtmlDecode(await Read(await ana.PostAsync("/customer/packages/shipments/new",
            "/customer/packages/shipments/estimate", new Dictionary<string, string>(building) { ["destination"] = "" }))), StringComparison.Ordinal);
        Assert.Contains("data-estimate=\"/customer/packages/shipments/estimate\"", await ana.GetStringAsync("/customer/packages/shipments/new"), StringComparison.Ordinal);

        // A shipment of hers with a measured package but no destination yet: no estimate; another customer's shipment: not found.
        (Guid parcel, Guid _) = await ReceivedAsync(anaId, "TBA0005554441");
        Guid shipment = Guid.Empty;
        await WithDatabaseAsync(async context => shipment = (await context.Parcels.SingleAsync(item => item.Id == parcel)).ShipmentId!.Value);
        Assert.Contains("No estimate yet", WebUtility.HtmlDecode(await ana.GetStringAsync("/customer/rates?shipment=" + shipment)), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync("/customer/rates?shipment=" + shipment)).StatusCode);
    }

    private async Task<Guid> CustomerIdAsync(string email)
    {
        Guid id = Guid.Empty;
        await WithDatabaseAsync(async context => id = (await context.Customers.SingleAsync(item => item.Email == EmailAddress.Create(email))).Id);
        return id;
    }

    private static byte[] Png()
    {
        byte[] data = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(data, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(16), 640);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(20), 480);
        return data;
    }

    private async Task<Browser> ConfirmedAsync(string email)
    {
        Browser browser = OpenBrowser();
        await browser.PostAsync("/customer/sign-up", "/customer/sign-up", new()
        {
            ["firstName"] = "Ana",
            ["lastName"] = "Pierre",
            ["email"] = email,
            ["phone"] = "+50937123456",
            ["language"] = "en",
            ["password"] = Password
        });
        string token = _api.Mailer.LastToken(email);
        HttpResponseMessage verified = await browser.PostAsync("/customer/verify?token=" + Uri.EscapeDataString(token), "/customer/verify", new() { ["token"] = token });
        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        return browser;
    }

    private static OperationsLocationDetails Details(string code, string name) =>
        new(code, name, OperationsPostalAddress.Create("8400 NW 25th St", null, "Doral", "FL", "33122", "US"), null, null, null, true);

    // Slice 7 step 2 (R-6): once the shipment is booked, the customer names who may collect it (same rules as the recipient), or no one; never another customer's.
    [SqlServerFact]
    public async Task Customer_NamesWhoMayCollect_OnceBooked()
    {
        Browser ana = await ConfirmedAsync("ana.collect@example.com");
        string shipmentPath = (await ana.PostAsync("/customer/packages/shipments/new", "/customer/packages/shipments/new", Shipment("TBA0003330001"))).Headers.Location!.OriginalString;
        Guid shipment = Guid.Parse(shipmentPath["/customer/packages/shipments/".Length..^"?done=created".Length]);
        string page = "/customer/packages/shipments/" + shipment;
        string collector = page + "/collector";
        Dictionary<string, string> cousin = new() { ["collectorName"] = "Marie Joseph", ["collectorPhone"] = "+50937111111" };
        Assert.DoesNotContain("name=\"collectorName\"", await ana.GetStringAsync(page), StringComparison.Ordinal);
        Assert.Contains("once the shipment is booked", WebUtility.HtmlDecode(await Read(await ana.PostAsync(page, collector, cousin))), StringComparison.Ordinal);

        await WithDatabaseAsync(context => context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ops].[Shipments] SET [Status] = 1 WHERE [Id] = {shipment}"));
        Assert.Contains("name=\"collectorName\"", await ana.GetStringAsync(page), StringComparison.Ordinal);
        Dictionary<string, string> local = new() { ["collectorName"] = "Marie Joseph", ["collectorPhone"] = "37111111" };
        Assert.DoesNotContain("done=collector", (await ana.PostAsync(page, collector, local)).Headers.Location?.OriginalString ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(page + "?done=collector", (await ana.PostAsync(page, collector, cousin)).Headers.Location?.OriginalString);
        Assert.Contains("value=\"Marie Joseph\"", await ana.GetStringAsync(page), StringComparison.Ordinal);

        // Another customer: not found.
        Browser ben = await ConfirmedAsync("ben.collect@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostAsync("/customer/packages", collector, cousin)).StatusCode);

        // No one: both cleared.
        Assert.Equal(page + "?done=collector", (await ana.PostAsync(page, collector, new() { ["collectorName"] = "", ["collectorPhone"] = "" })).Headers.Location?.OriginalString);
        Assert.DoesNotContain("value=\"Marie Joseph\"", await ana.GetStringAsync(page), StringComparison.Ordinal);
    }

    private async Task WithDatabaseAsync(Func<OperationsDbContext, Task> work)
    {
        await using AsyncServiceScope scope = _api.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(_tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        await work(context);
        await context.SaveChangesAsync();
    }

    private Browser OpenBrowser() => new(new HttpClient(_portal.GetTestServer().CreateHandler()) { BaseAddress = new Uri($"https://{_slug}.techcrafters.ltd/") });

    private static Task<string> Read(HttpResponseMessage response) => response.Content.ReadAsStringAsync();

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"|type=\"hidden\" name=\"__RequestVerificationToken\" value=\"([^\"]+)\"|name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex FormToken();

    // A browser with a cookie jar that never follows redirects.
    private sealed class Browser
    {
        private readonly HttpClient _client;

        public Browser(HttpClient client)
        {
            _client = client;
        }

        public Dictionary<string, string> Cookies { get; } = new(StringComparer.Ordinal);

        public async Task<string> GetStringAsync(string path) => await (await SendAsync(HttpMethod.Get, path, null)).Content.ReadAsStringAsync();

        public Task<HttpResponseMessage> GetAsync(string path) => SendAsync(HttpMethod.Get, path, null);

        // Posts with the token of a page already loaded, as a browser does on a second click.
        public Task<HttpResponseMessage> PostFromAsync(string page, string postPath, Dictionary<string, string> form)
        {
            Match token = FormToken().Match(page);
            Assert.True(token.Success, "The page has no antiforgery token.");
            return SendAsync(HttpMethod.Post, postPath, new Dictionary<string, string>(form) { ["__RequestVerificationToken"] = token.Groups.Values.Skip(1).First(group => group.Success).Value });
        }

        public async Task<HttpResponseMessage> PostAsync(string pagePath, string postPath, Dictionary<string, string> form)
        {
            string page = await GetStringAsync(pagePath);
            Match token = FormToken().Match(page);
            Assert.True(token.Success, "The page " + pagePath + " has no antiforgery token.");
            return await SendAsync(HttpMethod.Post, postPath, new Dictionary<string, string>(form) { ["__RequestVerificationToken"] = token.Groups.Values.Skip(1).First(group => group.Success).Value });
        }

        // Posts a form with one file, as the browser does for enctype="multipart/form-data".
        public async Task<HttpResponseMessage> PostFileAsync(string pagePath, string postPath, Dictionary<string, string> form, string fileName, byte[] content, string contentType,
            string fileField = "document")
        {
            Match token = FormToken().Match(await GetStringAsync(pagePath));
            Assert.True(token.Success, "The page " + pagePath + " has no antiforgery token.");
            MultipartFormDataContent body = new();

            foreach (KeyValuePair<string, string> field in new Dictionary<string, string>(form) { ["__RequestVerificationToken"] = token.Groups.Values.Skip(1).First(group => group.Success).Value })
            {
                body.Add(new StringContent(field.Value), field.Key);
            }

            ByteArrayContent file = new(content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            body.Add(file, fileField, fileName);
            return await SendAsync(HttpMethod.Post, postPath, null, body);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Dictionary<string, string>? form, HttpContent? content = null)
        {
            using HttpRequestMessage request = new(method, path);

            if (form is not null)
            {
                request.Content = new FormUrlEncodedContent(form);
            }
            else if (content is not null)
            {
                request.Content = content;
            }

            if (Cookies.Count > 0)
            {
                request.Headers.Add("Cookie", string.Join("; ", Cookies.Select(pair => pair.Key + "=" + pair.Value)));
            }

            HttpResponseMessage response = await _client.SendAsync(request);

            foreach (string cookie in response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values) ? values : Array.Empty<string>())
            {
                string pair = cookie.Split(';')[0];
                int equals = pair.IndexOf('=', StringComparison.Ordinal);

                if (pair[(equals + 1)..].Length == 0)
                {
                    Cookies.Remove(pair[..equals]);
                }
                else
                {
                    Cookies[pair[..equals]] = pair[(equals + 1)..];
                }
            }

            return response;
        }
    }
}
