using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OtpNet;
using Vixly.Contracts.Operations.Printing;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Web.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsParcelHttpTests.cs
//
// PURPOSE:
// Proves the dock, the inspection station, the label printers and the parcel pages over HTTP with a real database (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Dock: carrier deliveries, one scan one label, repeats and reprints, the delivery receipt
// - Matching by pre-alert only; conflicts and unknown packages go to the unidentified queue, then assignment with a recorded check
// - Inspection station: condition, photos, weight and size, the random compliance draw
// - Printers and the print program: jobs queued by scans, reprints and tests, taken and finished with the program's key
// - Staff without these permissions see and do nothing
// - Slice 7: destination receiving: bundles then parcels, customs, ready for pickup, discrepancies, a hub and an onward dispatch
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER
// - The scan app's calls carry the antiforgery token in a header, as ops-scan.js sends it
// - The customer email leaves through the outbox; the test runs the handler as the worker would
// =========================================================
public sealed partial class OperationsParcelHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsParcelHttpTests(OperationsWebDatabase database)
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
    public async Task Dock_ScansCarrierBarcodes_OneLabelPerScan_AndClosesTheDeliveryWithAReceipt()
    {
        Guid tenant = NewTenant("dockco", "Dock Co");
        using CookieClient owner = await OwnerAsync("dockco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        await PreAlertAsync(tenant, ana, "1Z999AA10123456784", "Two pairs of shoes");
        await PreAlertAsync(tenant, ana, "9400111899223397846256", "Vitamins");

        // The scan app may use the camera; it opens a delivery once, even when the button is pressed twice.
        (HttpResponseMessage page, string station) = await owner.GetAsync("/staff/scan?warehouse=" + warehouse);
        Assert.Contains("camera=(self)", page.Headers.GetValues("Permissions-Policy").Single(), StringComparison.Ordinal);
        Assert.Contains("data-warehouse=\"" + warehouse + "\"", station, StringComparison.Ordinal);
        string openKey = NewKey();
        (HttpResponseMessage opened, JsonElement delivery) = await owner.PostJsonAsync("/staff/scan/api/deliveries", station, new { warehouseId = warehouse, carrier = "UPS", key = openKey });
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        Guid deliveryId = delivery.GetProperty("deliveryId").GetGuid();
        (HttpResponseMessage _, JsonElement again) = await owner.PostJsonAsync("/staff/scan/api/deliveries", station, new { warehouseId = warehouse, carrier = "UPS", key = openKey });
        Assert.Equal(deliveryId, again.GetProperty("deliveryId").GetGuid());
        (HttpResponseMessage noToken, JsonElement _) = await owner.PostJsonAsync("/staff/scan/api/deliveries", string.Empty, new { warehouseId = warehouse, carrier = "UPS", key = NewKey() });
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);

        // 1. The pre-alert matches: the customer's code comes back for the label, and the customer's declaration goes on the parcel.
        string scanKey = NewKey();
        JsonElement first = await ScanOkAsync(owner, station, deliveryId, "1z999aa1 0123 456784", scanKey);
        Assert.Equal("pre_alert", first.GetProperty("outcome").GetString());
        Assert.Equal(anaCode, first.GetProperty("customerCode").GetString());
        Assert.Matches("^PK\\d{8}$", first.GetProperty("number").GetString());
        Assert.False(first.GetProperty("queued").GetBoolean());
        Assert.Contains("<svg", first.GetProperty("barcode").GetString(), StringComparison.Ordinal);
        Guid parcel = first.GetProperty("parcelId").GetGuid();

        // The same scan sent twice (a retry, the offline queue) counts once; the package scanned again only prints its label again.
        JsonElement repeat = await ScanOkAsync(owner, station, deliveryId, "1Z999AA10123456784", scanKey);
        Assert.Equal("repeat", repeat.GetProperty("outcome").GetString());
        Assert.Equal(parcel, repeat.GetProperty("parcelId").GetGuid());
        JsonElement reprint = await ScanOkAsync(owner, station, deliveryId, "1Z999AA10123456784");
        Assert.Equal("reprint", reprint.GetProperty("outcome").GetString());
        Assert.Equal(parcel, reprint.GetProperty("parcelId").GetGuid());

        // 2. A USPS barcode with its ZIP prefix finds the number the customer announced.
        JsonElement usps = await ScanOkAsync(owner, station, deliveryId, "420331229400111899223397846256");
        Assert.Equal("pre_alert", usps.GetProperty("outcome").GetString());
        Guid second = usps.GetProperty("parcelId").GetGuid();

        // 3. Nothing announced: unidentified, and damage seen at the dock opens a hold at once.
        JsonElement unknown = await ScanOkAsync(owner, station, deliveryId, "TBA123456789000", damaged: true);
        Assert.Equal("unidentified", unknown.GetProperty("outcome").GetString());
        Assert.Equal(JsonValueKind.Null, unknown.GetProperty("customerCode").ValueKind);
        Guid orphan = unknown.GetProperty("parcelId").GetGuid();
        (HttpResponseMessage unreadable, JsonElement refused) = await ScanAsync(owner, station, deliveryId, "AB");
        Assert.Equal(HttpStatusCode.BadRequest, unreadable.StatusCode);
        Assert.Equal("This barcode is not a carrier tracking number.", refused.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await ScanAsync(owner, station, deliveryId, "42033122")).Response.StatusCode);

        // The parcel's own ID label scanned by mistake prints it again; it never becomes a parcel of its own.
        JsonElement ownLabel = await ScanOkAsync(owner, station, deliveryId, first.GetProperty("number").GetString()!);
        Assert.Equal("reprint", ownLabel.GetProperty("outcome").GetString());
        Assert.Equal(parcel, ownLabel.GetProperty("parcelId").GetGuid());

        (HttpResponseMessage _, string firstPage) = await owner.GetAsync("/staff/parcels/" + parcel);
        Assert.Contains("1Z999AA10123456784", firstPage, StringComparison.Ordinal);
        Assert.Contains("Two pairs of shoes", firstPage, StringComparison.Ordinal);
        Assert.Contains("Pre-alerted by the customer", firstPage, StringComparison.Ordinal);
        Assert.Contains("Received at WH1", firstPage, StringComparison.Ordinal);
        Assert.DoesNotContain("On hold", firstPage, StringComparison.Ordinal);

        // Staff read where the shipment goes, as the customer wrote it (owner, 2026-10-18).
        Assert.Contains("Where the shipment goes", firstPage, StringComparison.Ordinal);
        Assert.Contains("12 Rue Capois, Port-au-Prince, HT", firstPage, StringComparison.Ordinal);
        Assert.Contains("Ana Pierre · +50937123456", firstPage, StringComparison.Ordinal);
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync("/staff/parcels/" + second);
        Assert.Contains("9400111899223397846256", secondPage, StringComparison.Ordinal);
        Assert.Contains("Vitamins", secondPage, StringComparison.Ordinal);

        // The email leaves through the outbox: "your package arrived", with the link to the package.
        await DispatchAsync(parcel);
        OperationsCustomerMail mail = Assert.Single(_host.CustomerMailer.Sent, item => item.ToAddress == "ana@example.com");
        Assert.Equal(OperationsCustomerMailKind.ParcelReceived, mail.Kind);
        Assert.EndsWith("/customer/packages/" + parcel, mail.Link, StringComparison.Ordinal);

        // Closing the delivery gives the receipt for the driver: three pieces, one damaged. Closing twice gives the same receipt.
        (HttpResponseMessage closed, JsonElement receipt) = await owner.PostJsonAsync("/staff/scan/api/deliveries/" + deliveryId + "/close", station, new { });
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        Guid receiptId = receipt.GetProperty("receiptId").GetGuid();
        string receiptNumber = receipt.GetProperty("number").GetString()!;
        (HttpResponseMessage _, JsonElement closedAgain) = await owner.PostJsonAsync("/staff/scan/api/deliveries/" + deliveryId + "/close", station, new { });
        Assert.Equal(receiptId, closedAgain.GetProperty("receiptId").GetGuid());
        HttpResponseMessage pdf = (await owner.GetAsync(receipt.GetProperty("url").GetString()!)).Response;
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(await pdf.Content.ReadAsByteArrayAsync())[..4], StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsWarehouseReceipt stored = await context.WarehouseReceipts.SingleAsync(item => item.DeliveryId == deliveryId);
            Assert.Equal(3, stored.PieceCount);
            Assert.Equal(1, stored.DamagedCount);
        }

        // A closed delivery takes no new package, but a package already here still gets its label again.
        (HttpResponseMessage late, JsonElement lateError) = await ScanAsync(owner, station, deliveryId, "TBA555666777888");
        Assert.Equal(HttpStatusCode.BadRequest, late.StatusCode);
        Assert.Equal("This delivery is closed. Open a new one to keep scanning.", lateError.GetProperty("message").GetString());
        Assert.Equal("reprint", (await ScanOkAsync(owner, station, deliveryId, "1Z999AA10123456784")).GetProperty("outcome").GetString());

        (HttpResponseMessage receivingResponse, string receiving) = await owner.GetAsync("/staff/receiving");
        Assert.Contains("camera=()", receivingResponse.Headers.GetValues("Permissions-Policy").Single(), StringComparison.Ordinal);
        Assert.Contains(receiptNumber, receiving, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/receipts/" + receiptId + "\"", receiving, StringComparison.Ordinal);

        // The unidentified queue and search; the damaged package is on hold.
        (HttpResponseMessage _, string orphanPage) = await owner.GetAsync("/staff/parcels/" + orphan);
        Assert.Contains("On hold: Damaged", orphanPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Where the shipment goes", orphanPage, StringComparison.Ordinal);
        (HttpResponseMessage _, string queue) = await owner.GetAsync("/staff/parcels/unidentified");
        Assert.Contains("TBA123456789000", queue, StringComparison.Ordinal);
        Assert.DoesNotContain("1Z999AA10123456784", queue, StringComparison.Ordinal);
        (HttpResponseMessage _, string search) = await owner.GetAsync("/staff/parcels?search=" + anaCode);
        Assert.Contains("1Z999AA10123456784", search, StringComparison.Ordinal);
        Assert.DoesNotContain("TBA123456789000", search, StringComparison.Ordinal);

        // Assigned only with what the customer proved, recorded in the history.
        (HttpResponseMessage _, string unproved) = await owner.PostAsync("/staff/parcels/" + orphan + "/assign", orphanPage, ("customer", anaCode), ("key", NewKey()));
        Assert.Contains("Choose what the customer gave", unproved, StringComparison.Ordinal);
        (HttpResponseMessage assigned, string _) = await owner.PostAsync("/staff/parcels/" + orphan + "/assign", orphanPage, ("customer", anaCode), ("verifiedBy", "order_number"), ("key", NewKey()));
        Assert.Equal("/staff/parcels/" + orphan + "?done=assigned", assigned.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string assignedPage) = await owner.GetAsync("/staff/parcels/" + orphan + "?done=assigned");
        Assert.Contains("Assigned to customer " + anaCode, assignedPage, StringComparison.Ordinal);
        Assert.Contains("Hold released: Unidentified", assignedPage, StringComparison.Ordinal);
        Assert.Contains("On hold: Waiting for the customer's information", assignedPage, StringComparison.Ordinal);

        // The two announced packages joined the shipment Ana announced them in; the one assigned later waits in a shipment without instructions (D-R1; owner, 2026-10-18).
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Guid? announcedIn = (await context.PreAlerts.FirstAsync(item => item.CustomerId == ana)).ShipmentId;
            Assert.Equal(announcedIn, (await context.Parcels.SingleAsync(item => item.Id == parcel)).ShipmentId);
            Assert.Equal(announcedIn, (await context.Parcels.SingleAsync(item => item.Id == second)).ShipmentId);
            Guid? waiting = (await context.Parcels.SingleAsync(item => item.Id == orphan)).ShipmentId;
            Assert.NotEqual(announcedIn, waiting);
            Assert.Null((await context.Shipments.SingleAsync(item => item.Id == waiting)).DestinationCountryCode);
            Assert.Equal(3, await context.Parcels.CountAsync(item => item.DeliveryId == deliveryId));
        }

        // The customer's page shows what is coming to them, never the street address of their mailbox (owner, 2026-10-18).
        await PreAlertAsync(tenant, ana, "TBA777888999000", "Headphones");
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync("/staff/customers/" + ana);
        Assert.Contains("Incoming packages", customerPage, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/parcels/" + parcel + "\"", customerPage, StringComparison.Ordinal);
        Assert.Contains("TBA777888999000", customerPage, StringComparison.Ordinal);
        Assert.Contains("Headphones", customerPage, StringComparison.Ordinal);
        Assert.DoesNotContain("8400 NW 25th St", customerPage, StringComparison.Ordinal);

        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=receiving");
        Assert.Contains("ops.receiving.received", history, StringComparison.Ordinal);
    }

    // Slice 4D (owner, 2026-10-19): the dock puts a package in the shipment its pre-alert is in, never by guesswork; one whose pre-alert is in no
    // shipment waits for the customer. Staff see every pre-alert in the incoming list, and export it.
    [SqlServerFact]
    public async Task Dock_PlacesAPackageInItsPreAlertsShipment_OrLetsItWait_AndIncomingListsEveryPreAlert()
    {
        Guid tenant = NewTenant("inbound", "Inbound Co");
        using CookieClient owner = await OwnerAsync("inbound");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        Guid second = Guid.Empty;
        Guid placed = Guid.Empty;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            IOperationsReceivingRepository receiving = scope.ServiceProvider.GetRequiredService<IOperationsReceivingRepository>();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Vixly.Operations.Domain.Shipments.OperationsShipment Shipment(string city) => Vixly.Operations.Domain.Shipments.OperationsShipment.Request(tenant, ana, warehouse,
                Vixly.Operations.Domain.Shipments.OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("1 Rue", null, city, null, null, "HT"), "Ana Pierre",
                    "+50937123456", Vixly.Operations.Domain.Shipments.OperationsShipmentOptions.None), now);
            Vixly.Operations.Domain.Shipments.OperationsShipment first = Shipment("Les Cayes");
            Vixly.Operations.Domain.Shipments.OperationsShipment other = Shipment("Jacmel");
            second = other.Id;
            receiving.Add(first);
            receiving.Add(other);
            OperationsPreAlert inOther = OperationsPreAlert.Announce(tenant, ana, warehouse, "UPS", "1Z999AA10123456788", "Shop", "Tape", 20m, "USD", DateOnly.FromDateTime(now.UtcDateTime), now);
            inOther.PlaceIn(other, now);
            placed = inOther.Id;
            receiving.Add(inOther);
            receiving.Add(OperationsPreAlert.Announce(tenant, ana, warehouse, "UPS", "1Z999AA10123456789", "Shop", "Boxes", 30m, "USD", null, now));
            receiving.Add(OperationsPreAlert.Announce(tenant, ana, warehouse, "FedEx", "123456789012", "Shop", "=HYPERLINK(\"x\")", 5m, "USD",
                DateOnly.FromDateTime(now.UtcDateTime).AddDays(-3), now));
            Assert.True(await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));
        }

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        Assert.Equal(anaCode, (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456788")).GetProperty("customerCode").GetString());
        Assert.Equal(anaCode, (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456789")).GetProperty("customerCode").GetString());

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsParcel tape = await context.Parcels.SingleAsync(item => item.CarrierTrackingNumber == "1Z999AA10123456788");
            OperationsParcel boxes = await context.Parcels.SingleAsync(item => item.CarrierTrackingNumber == "1Z999AA10123456789");
            Assert.Equal(second, tape.ShipmentId);
            Assert.False(tape.HoldReasons.HasFlag(OperationsHoldReasons.AwaitingShipment));
            Assert.True(boxes.HoldReasons.HasFlag(OperationsHoldReasons.AwaitingShipment));
            Assert.Equal("Boxes", boxes.DeclaredContents);
            Assert.False((await context.Shipments.SingleAsync(item => item.Id == boxes.ShipmentId)).HasInstructions);
        }

        (HttpResponseMessage _, string received) = await owner.GetAsync("/staff/parcels/incoming?view=received");
        Assert.Contains("1Z999AA10123456788", received, StringComparison.Ordinal);
        Assert.Contains("1Z999AA10123456789", received, StringComparison.Ordinal);
        Assert.DoesNotContain("123456789012", received, StringComparison.Ordinal);
        (HttpResponseMessage _, string overdue) = await owner.GetAsync("/staff/parcels/incoming?view=overdue");
        Assert.Contains("123456789012", overdue, StringComparison.Ordinal);
        Assert.Matches("<span>Past expected day</span>\\s*<span class=\"ops-status-chip__count\">1<", System.Net.WebUtility.HtmlDecode(overdue));
        Assert.Matches("<span>Received</span>\\s*<span class=\"ops-status-chip__count\">2<", System.Net.WebUtility.HtmlDecode(overdue));

        // The export: the same rows; a formula typed by a customer stays text.
        (HttpResponseMessage file, string csv) = await owner.GetAsync("/staff/parcels/incoming/export?view=all");
        Assert.True(file.IsSuccessStatusCode, file.StatusCode + " " + file.Headers.Location);
        Assert.Equal("text/csv", file.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"'=HYPERLINK(\"\"x\"\")\"", csv, StringComparison.Ordinal);
        Assert.Equal(4, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.NotEqual(Guid.Empty, placed);
    }

    [SqlServerFact]
    public async Task OneNumberAnnouncedByTwoCustomers_LeavesTheParcelUnidentified()
    {
        Guid tenant = NewTenant("dispco", "Dispute Co");
        using CookieClient owner = await OwnerAsync("dispco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        (Guid ben, string _) = await CustomerAsync(tenant, "ben@example.com");
        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "Amazon");

        // Nobody can take a neighbour's package by announcing its number too (review H1): a clerk checks first.
        await PreAlertAsync(tenant, ana, "TBA444333222", "Shoes");
        await PreAlertAsync(tenant, ben, "TBA444333222", "Shoes too");
        JsonElement two = await ScanOkAsync(owner, station, delivery, "TBA444333222");
        Assert.Equal("conflict", two.GetProperty("outcome").GetString());
        Assert.Equal(JsonValueKind.Null, two.GetProperty("customerCode").ValueKind);

        // One customer's announcement, even spelled with spaces, is that customer's package.
        await PreAlertAsync(tenant, ana, "TBA111222333", "Bag");
        JsonElement one = await ScanOkAsync(owner, station, delivery, "tba 111 222 333");
        Assert.Equal(anaCode, one.GetProperty("customerCode").GetString());

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        Assert.Equal(1, await context.Parcels.CountAsync(item => item.CustomerId == null));
        Assert.Equal(2, await context.PreAlerts.CountAsync(item => item.Status == OperationsPreAlertStatus.Open));
    }

    [SqlServerFact]
    public async Task InspectionStation_ChecksCondition_Weighs_Measures_AndDrawsForCompliance()
    {
        Guid tenant = NewTenant("inspco", "Inspect Co");
        using CookieClient owner = await OwnerAsync("inspco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string code) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456785", "Books");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456786", "Lamp");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456787", "Glasses");
        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        string number = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456785")).GetProperty("number").GetString()!;
        string drawn = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456786")).GetProperty("number").GetString()!;
        Guid broken = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456787")).GetProperty("parcelId").GetGuid();

        // The clerk scans the ID label (or the carrier barcode): the parcel, its customer and shipment are shown.
        (HttpResponseMessage _, string start) = await owner.GetAsync("/staff/inspection");
        Assert.Contains("name=\"code\"", start, StringComparison.Ordinal);
        (HttpResponseMessage found, string _) = await owner.GetAsync("/staff/inspection?code=" + number.ToLowerInvariant());
        string path = found.Headers.Location!.OriginalString;
        Guid parcel = Guid.Parse(path[^36..]);
        Assert.Equal(path, (await owner.GetAsync("/staff/inspection?code=1Z999AA10123456785")).Response.Headers.Location?.OriginalString);
        Assert.Equal(path, (await owner.GetAsync("/staff/inspection?code=" + Uri.EscapeDataString("]C1" + number))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string missing) = await owner.GetAsync("/staff/inspection?code=PK00000000");
        Assert.Contains("role=\"alert\"", missing, StringComparison.Ordinal);

        (HttpResponseMessage _, string form) = await owner.GetAsync(path);
        Assert.Contains(code, form, StringComparison.Ordinal);
        Assert.Contains("Books", form, StringComparison.Ordinal);
        Assert.Contains(drawn, form, StringComparison.Ordinal);

        // A photo of the package is required the first time; one of the label alone is not enough.
        (HttpResponseMessage _, string noPhoto) = await CheckAsync(owner, path, form, "intact", new[] { "photo_label" });
        Assert.Contains("Add a photo of the package", noPhoto, StringComparison.Ordinal);
        (HttpResponseMessage checkedOk, string _) = await CheckAsync(owner, path, form, "intact", new[] { "photo_package", "photo_label" });
        Assert.Equal("/staff/inspection?done=checked&last=" + number, checkedOk.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string next) = await owner.GetAsync(checkedOk.Headers.Location!.OriginalString);
        Assert.Contains(number + " checked.", next, StringComparison.Ordinal);

        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/parcels/" + parcel);
        Assert.Contains("Checked: 2.5 kg", page, StringComparison.Ordinal);
        Assert.Contains("Condition recorded: Intact", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Selected for compliance inspection", page, StringComparison.Ordinal);

        // The random draw sets some parcels aside for compliance inspection.
        _host.Draw.Picks = true;
        string drawnForm = (await owner.GetAsync("/staff/inspection?code=" + drawn)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string drawnHtml) = await owner.GetAsync(drawnForm);
        (HttpResponseMessage selected, string _) = await CheckAsync(owner, drawnForm, drawnHtml, "intact", new[] { "photo_package" });
        _host.Draw.Picks = false;
        Assert.Equal("/staff/inspection?done=selected&last=" + drawn, selected.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string aside) = await owner.GetAsync(selected.Headers.Location!.OriginalString);
        Assert.Contains("selected for compliance inspection: set it aside", aside, StringComparison.Ordinal);

        // Damage found at the station needs its photo and puts the parcel on hold.
        (HttpResponseMessage _, string brokenForm) = await owner.GetAsync("/staff/inspection/" + broken);
        (HttpResponseMessage _, string noDamagePhoto) = await CheckAsync(owner, "/staff/inspection/" + broken, brokenForm, "damaged", new[] { "photo_package" });
        Assert.Contains("Add a photo of the package, and of the damage", noDamagePhoto, StringComparison.Ordinal);
        (HttpResponseMessage damaged, string _) = await CheckAsync(owner, "/staff/inspection/" + broken, brokenForm, "damaged", new[] { "photo_package", "photo_damage" });
        Assert.StartsWith("/staff/inspection?done=", damaged.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string brokenPage) = await owner.GetAsync("/staff/parcels/" + broken);
        Assert.Contains("On hold: Damaged", brokenPage, StringComparison.Ordinal);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        OperationsParcel measured = await context.Parcels.SingleAsync(item => item.Id == parcel);
        Assert.Equal(2.5m, measured.WeightKg);
        Assert.Equal(10.5m, measured.HeightCm);
        Assert.Equal(1, await context.TrackingEvents.CountAsync(item => item.Code == OperationsTrackingCodes.InspectionRequired));
        Assert.Equal(5, await context.ParcelPhotos.CountAsync());
    }

    // Slice 4D step 2 (A-5): inspection prints the shipping label of a cleared parcel at once; one cleared later waits in the labels to print.
    [SqlServerFact]
    public async Task Inspection_PrintsTheShippingLabelOfAClearedParcel_AndTheOthersWaitInLabelsToPrint()
    {
        Guid tenant = NewTenant("labelco", "Label Co");
        using CookieClient owner = await OwnerAsync("labelco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456785", "Books");
        Guid shippingPrinter = Guid.Empty;
        Guid idPrinter = Guid.Empty;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Printing.OperationsLabelPrinter shipping = Vixly.Operations.Domain.Printing.OperationsLabelPrinter.Create(tenant, warehouse, "Inspection 1",
                Vixly.Operations.Domain.Printing.OperationsPrinterConnection.Network, "192.168.1.60:9100", 203, DateTimeOffset.UtcNow, Vixly.Operations.Domain.Printing.OperationsLabelMedia.ShippingLabel);
            Vixly.Operations.Domain.Printing.OperationsLabelPrinter dock = Vixly.Operations.Domain.Printing.OperationsLabelPrinter.Create(tenant, warehouse, "Dock 1",
                Vixly.Operations.Domain.Printing.OperationsPrinterConnection.Network, "192.168.1.61:9100", 203, DateTimeOffset.UtcNow);
            (shippingPrinter, idPrinter) = (shipping.Id, dock.Id);
            context.AddRange(shipping, dock);
            // The second package's pre-alert is in no shipment yet.
            context.Add(OperationsPreAlert.Announce(tenant, lee, warehouse, "UPS", "1Z999AA10123456786", null, "Lamp", 40m, "USD", null, DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        Guid cleared = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456785")).GetProperty("parcelId").GetGuid();
        Guid waiting = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456786")).GetProperty("parcelId").GetGuid();

        // The station offers only the printers holding shipping labels.
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/inspection/" + cleared);
        Assert.Contains("name=\"shippingPrinter\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("Dock 1", form, StringComparison.Ordinal);
        (HttpResponseMessage labelled, string _) = await StationCheckAsync(owner, cleared, form, shippingPrinter);
        Assert.StartsWith("/staff/inspection?done=labelled", labelled.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string waitingForm) = await owner.GetAsync("/staff/inspection/" + waiting);
        (HttpResponseMessage notLabelled, string _) = await StationCheckAsync(owner, waiting, waitingForm, shippingPrinter);
        Assert.StartsWith("/staff/inspection?done=checked", notLabelled.Headers.Location?.OriginalString, StringComparison.Ordinal);

        string shipmentNumber = string.Empty;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsParcel first = await context.Parcels.SingleAsync(item => item.Id == cleared);
            Assert.NotNull(first.ShippingLabelUtc);
            shipmentNumber = (await context.Shipments.SingleAsync(item => item.Id == first.ShipmentId)).Number!;
            Assert.StartsWith("SH", shipmentNumber, StringComparison.Ordinal);
            string zpl = Assert.Single(await context.Set<Vixly.Operations.Domain.Printing.OperationsPrintJob>().Where(item => item.PrinterId == shippingPrinter).ToListAsync()).Payload;
            Assert.Contains("^FD" + shipmentNumber + "^FS", zpl, StringComparison.Ordinal);
            Assert.Contains("1 / 1", zpl, StringComparison.Ordinal);
            Assert.Null((await context.Parcels.SingleAsync(item => item.Id == waiting)).ShippingLabelUtc);
        }

        // Not cleared: no label, and not in the labels to print; a dock printer never prints a shipping label.
        (HttpResponseMessage _, string waitingPage) = await owner.GetAsync("/staff/parcels/" + waiting);
        Assert.Contains("Printed once the parcel is cleared", waitingPage, StringComparison.Ordinal);
        (HttpResponseMessage refused, string _) = await owner.PostAsync("/staff/parcels/" + cleared + "/shipping-label", waitingPage, ("printer", idPrinter.ToString()));
        Assert.Contains("error=ops.printing.printer_not_found", refused.Headers.Location?.OriginalString, StringComparison.Ordinal);

        // Put in the shipment by the customer: cleared now, it waits in the labels to print until printed.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            IServiceProvider services = scope.ServiceProvider;
            OperationsDbContext context = services.GetRequiredService<OperationsDbContext>();
            Guid shipmentId = (await context.Parcels.SingleAsync(item => item.Id == cleared)).ShipmentId!.Value;
            OperationsParcel parcel = (await services.GetRequiredService<IOperationsTrackingRepository>().GetParcelAsync(waiting, CancellationToken.None))!;
            Vixly.Operations.Domain.Shipments.OperationsShipment target = (await services.GetRequiredService<IOperationsReceivingRepository>().GetShipmentAsync(shipmentId, CancellationToken.None))!;
            Assert.Null(await services.GetRequiredService<Vixly.Operations.Application.Shipments.OperationsPackageMover>().MoveParcelAsync(parcel, target, "mov:test:" + Guid.NewGuid().ToString("N"),
                OperationsTrackingActor.Customer, OperationsTrackingSource.Customer, null, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.True(await services.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));
        }

        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/parcels/labels");
        Assert.Contains("/staff/parcels/" + waiting, list, StringComparison.Ordinal);
        Assert.Contains("Print them all", list, StringComparison.Ordinal);
        Assert.DoesNotContain("/staff/parcels/" + cleared + "\"", list, StringComparison.Ordinal);
        (HttpResponseMessage all, string _) = await owner.PostAsync("/staff/parcels/labels/print", list, ("warehouse", warehouse.ToString()), ("printer", shippingPrinter.ToString()));
        Assert.Contains("done=all_printed&count=1", all.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string empty) = await owner.GetAsync("/staff/parcels/labels?warehouse=" + warehouse);
        Assert.Contains("Every cleared parcel has its shipping label.", empty, StringComparison.Ordinal);

        // The PDF to print from the browser.
        (HttpResponseMessage _, string clearedPage) = await owner.GetAsync("/staff/parcels/" + cleared);
        (HttpResponseMessage pdf, string _) = await owner.PostAsync("/staff/parcels/" + cleared + "/shipping-label/pdf", clearedPage);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            List<string> jobs = await context.Set<Vixly.Operations.Domain.Printing.OperationsPrintJob>().Where(item => item.PrinterId == shippingPrinter).Select(item => item.Payload).ToListAsync();
            Assert.Equal(2, jobs.Count);
            Assert.Contains(jobs, job => job.Contains("2 / 2", StringComparison.Ordinal));
        }
    }

    // Slice 4D step 3: undeclared parcels: what the label shows, a customer found by the mailbox code on it, the storage fee, the count, an auction.
    [SqlServerFact]
    public async Task UndeclaredParcels_AreInventoried_AssignedByTheirLabel_Counted_Charged_AndSoldAtAuction()
    {
        Guid tenant = NewTenant("undeclco", "Undeclared Co");
        using CookieClient owner = await OwnerAsync("undeclco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid _, string anaCode) = await CustomerAsync(tenant, "ana@example.com");

        // The storage terms: 10 free days, then 1.25 a day; a package keeps the terms in force when it arrived.
        (HttpResponseMessage _, string holds) = await owner.GetAsync("/staff/holds");
        Assert.Equal("/staff/holds?done=saved", (await owner.PostAsync("/staff/holds/storage-fee", holds, ("freeDays", "10"), ("fee", "1.25"))).Response.Headers.Location?.OriginalString);

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "Amazon");
        Guid found = (await ScanOkAsync(owner, station, delivery, "TBA999000111")).GetProperty("parcelId").GetGuid();
        Guid stray = (await ScanOkAsync(owner, station, delivery, "TBA999000222")).GetProperty("parcelId").GetGuid();
        (HttpResponseMessage _, string strayForm) = await owner.GetAsync("/staff/inspection/" + stray);
        await CheckAsync(owner, "/staff/inspection/" + stray, strayForm, "intact", new[] { "photo_package" });

        // The clerk copies the label; the mailbox code on it points to Ana, checked before assigning.
        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/parcels/" + found);
        Assert.Contains("What the label shows", page, StringComparison.Ordinal);
        (HttpResponseMessage recorded, string _) = await owner.PostAsync("/staff/parcels/" + found + "/label-text", page, ("name", "Ana P."), ("code", anaCode.ToLowerInvariant()), ("sender", "Shoe store"));
        Assert.Equal("/staff/parcels/" + found + "?done=label_recorded", recorded.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string labelled) = await owner.GetAsync("/staff/parcels/" + found);
        Assert.Contains("The code on the label is " + anaCode, labelled, StringComparison.Ordinal);
        (HttpResponseMessage _, string byCode) = await owner.GetAsync("/staff/parcels/unidentified?search=" + anaCode);
        Assert.Contains("/staff/parcels/" + found, byCode, StringComparison.Ordinal);
        (HttpResponseMessage _, string byName) = await owner.GetAsync("/staff/parcels/unidentified?search=" + Uri.EscapeDataString("Shoe store"));
        Assert.Contains("/staff/parcels/" + found, byName, StringComparison.Ordinal);
        Assert.DoesNotContain("/staff/parcels/" + stray + "\"", byName, StringComparison.Ordinal);
        (HttpResponseMessage assigned, string _) = await owner.PostAsync("/staff/parcels/" + found + "/assign", labelled, ("customer", anaCode), ("verifiedBy", "label_code"), ("key", NewKey()));
        Assert.Equal("/staff/parcels/" + found + "?done=assigned", assigned.Headers.Location?.OriginalString);

        // The count of the undeclared area expects only the stray package now; scanned, it is found.
        (HttpResponseMessage _, string counts) = await owner.GetAsync("/staff/parcels/counts");
        (HttpResponseMessage started, string _) = await owner.PostAsync("/staff/parcels/counts", counts, ("warehouse", warehouse.ToString()));
        string countPath = started.Headers.Location!.OriginalString;
        Assert.Matches("^/staff/parcels/counts/[0-9a-f-]{36}$", countPath);
        Assert.Equal(countPath, (await owner.PostAsync("/staff/parcels/counts", counts, ("warehouse", warehouse.ToString()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string count) = await owner.GetAsync(countPath);
        Assert.Contains("0 found, 0 missing, of 1", count, StringComparison.Ordinal);
        Assert.Contains("/staff/parcels/" + stray, count, StringComparison.Ordinal);
        Assert.DoesNotContain("/staff/parcels/" + found + "\"", count, StringComparison.Ordinal);
        string strayNumber = Regex.Match(count, "href=\"/staff/parcels/" + stray + "\">(?<number>PK[0-9]+)</a>").Groups["number"].Value;
        (HttpResponseMessage marked, string _) = await owner.PostAsync(countPath + "/mark", count, ("code", strayNumber.ToLowerInvariant()));
        Assert.Equal(countPath + "?done=marked", marked.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string afterMark) = await owner.GetAsync(countPath);
        Assert.Contains("1 found, 0 missing, of 1", afterMark, StringComparison.Ordinal);
        Assert.Equal(countPath + "?done=closed", (await owner.PostAsync(countPath + "/close", afterMark)).Response.Headers.Location?.OriginalString);

        // A second count marks it missing: a note is needed, then the Owner closes it.
        (HttpResponseMessage second, string _) = await owner.PostAsync("/staff/parcels/counts", counts, ("warehouse", warehouse.ToString()));
        string secondPath = second.Headers.Location!.OriginalString;
        Assert.NotEqual(countPath, secondPath);
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync(secondPath);
        await owner.PostAsync(secondPath + "/mark", secondPage, ("parcel", stray.ToString()), ("result", "missing"));
        (HttpResponseMessage _, string noNote) = await owner.PostAsync(secondPath + "/close", secondPage);
        Assert.Contains("A missing parcel needs a note.", noNote, StringComparison.Ordinal);
        await owner.PostAsync(secondPath + "/mark", secondPage, ("parcel", stray.ToString()), ("result", "missing"), ("note", "Not on its shelf"));
        Assert.Equal(secondPath + "?done=closed", (await owner.PostAsync(secondPath + "/close", secondPage)).Response.Headers.Location?.OriginalString);

        // The storage fee after 15 days; a later change of the terms never applies backwards.
        (HttpResponseMessage _, string later) = await owner.GetAsync("/staff/holds");
        await owner.PostAsync("/staff/holds/storage-fee", later, ("freeDays", "0"), ("fee", "9.00"));
        await AgeAsync(tenant, stray, 15);
        (HttpResponseMessage _, string charged) = await owner.GetAsync("/staff/parcels/" + stray);
        Assert.Contains("6.25 USD so far (5 days)", charged, StringComparison.Ordinal);

        // Unclaimed after 30 days: sold at auction, with a note.
        await AgeAsync(tenant, stray, 35);
        (HttpResponseMessage _, string unclaimed) = await owner.GetAsync("/staff/holds");
        Assert.Equal("/staff/holds?done=decided", (await owner.PostAsync("/staff/holds/unclaimed/" + stray, unclaimed, ("decision", "auction"), ("note", "Lot 12, 40.00"), ("key", NewKey())))
            .Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string sold) = await owner.GetAsync("/staff/parcels/" + stray);
        Assert.Contains("Sold at auction", sold, StringComparison.Ordinal);
    }

    // Slice 4D step 4: a customer's ticket about a missing package; staff see the lookalikes, answer, and give the one found to the customer.
    [SqlServerFact]
    public async Task Tickets_ShowTheUndeclaredLookalikes_AnswerTheCustomer_AndGiveThePackageFound()
    {
        Guid tenant = NewTenant("ticketco", "Ticket Co");
        using CookieClient owner = await OwnerAsync("ticketco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "Amazon");
        Guid found = (await ScanOkAsync(owner, station, delivery, "TBA777000111")).GetProperty("parcelId").GetGuid();
        (string otherStation, Guid other) = await OpenAsync(owner, warehouse, "UPS");
        Guid stray = (await ScanOkAsync(owner, otherStation, other, "1Z999AA10123456784")).GetProperty("parcelId").GetGuid();
        Guid ticketId;

        // Ana knows only the end of the tracking number; she sent the carrier's proof.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Vixly.Operations.Domain.Tickets.OperationsTicket ticket = Vixly.Operations.Domain.Tickets.OperationsTicket.Open(tenant, ana, warehouse,
                new Vixly.Operations.Domain.Tickets.OperationsTicketDetails("Amazon", "XYZ0777000111", "Amazon", "113-1", DateOnly.FromDateTime(now.UtcDateTime), "Tape"), "Front desk.", now);
            string key = await scope.ServiceProvider.GetRequiredService<IOperationsFileStore>().SaveAsync(tenant, "tickets", Png(), "image/png", CancellationToken.None);
            ticket.AttachProof(key, "image/png", new string('a', 64), now);
            scope.ServiceProvider.GetRequiredService<IOperationsTicketRepository>().Add(ticket);
            Assert.True(await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));
            ticketId = ticket.Id;
        }

        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/tickets");
        Assert.Contains("/staff/tickets/" + ticketId, list, StringComparison.Ordinal);
        Assert.Contains("Waiting for us", list, StringComparison.Ordinal);
        string path = "/staff/tickets/" + ticketId;
        (HttpResponseMessage _, string page) = await owner.GetAsync(path);
        Assert.Contains(anaCode + " · Ana Pierre", page, StringComparison.Ordinal);
        Assert.Contains("Front desk.", page, StringComparison.Ordinal);
        Assert.Contains("/staff/parcels/" + found, page, StringComparison.Ordinal);
        Assert.DoesNotContain("/staff/parcels/" + stray, page, StringComparison.Ordinal);

        // The proof is the customer's file, served so it can never run as a page.
        (HttpResponseMessage proof, string _) = await owner.GetRawAsync(path + "/proof");
        Assert.Equal("image/png", proof.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", proof.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("default-src 'none'; sandbox", proof.Headers.GetValues("Content-Security-Policy").Single());

        // An answer: the ticket waits for Ana, and she gets an email with the link to it.
        // Sent twice (a double click): one message, one email.
        string answerKey = NewKey();
        (HttpResponseMessage answered, string _) = await owner.PostAsync(path + "/answer", page, ("message", "Which name was on the label?"), ("key", answerKey));
        Assert.Equal(path + "?done=answered", answered.Headers.Location?.OriginalString);
        Assert.Equal(path + "?done=answered", (await owner.PostAsync(path + "/answer", page, ("message", "Which name was on the label?"), ("key", answerKey))).Response.Headers.Location?.OriginalString);
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsOutboxMessage message = await context.OutboxMessages.SingleAsync(item => item.StreamId == ticketId);
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            await scope.ServiceProvider.GetServices<IOperationsOutboxHandler>().Single(item => item.MessageType == message.Type).HandleAsync(message, CancellationToken.None);
        }

        OperationsCustomerMail mail = Assert.Single(_host.CustomerMailer.Sent, item => item.ToAddress == "ana@example.com");
        Assert.Equal(OperationsCustomerMailKind.TicketAnswered, mail.Kind);
        Assert.EndsWith("/customer/tickets/" + ticketId, mail.Link, StringComparison.Ordinal);

        // Another customer's package is never linked; the one found is given to Ana with how staff checked it, and the ticket is resolved.
        (HttpResponseMessage _, string again) = await owner.GetAsync(path);
        Assert.Single(Regex.Matches(again, "Which name was on the label\\?"));
        (HttpResponseMessage _, string refused) = await owner.PostAsync(path + "/answer", again, ("message", " "), ("key", NewKey()));
        Assert.Contains("Write a message of up to 1,000 characters.", refused, StringComparison.Ordinal);
        (HttpResponseMessage _, string notTheirs) = await owner.PostAsync(path + "/link", again, ("parcel", stray.ToString()), ("verifiedBy", "made_up"), ("key", NewKey()));
        Assert.DoesNotContain("ticket is resolved", notTheirs, StringComparison.Ordinal);
        (HttpResponseMessage linked, string _) = await owner.PostAsync(path + "/link", again, ("parcel", found.ToString()), ("verifiedBy", "order_number"), ("key", NewKey()));
        Assert.Equal(path + "?done=linked", linked.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string resolved) = await owner.GetAsync(path + "?done=linked");
        Assert.Contains("The package is the customer’s now and the ticket is resolved.", resolved, StringComparison.Ordinal);
        Assert.Contains("Resolved", resolved, StringComparison.Ordinal);
        (HttpResponseMessage _, string parcelPage) = await owner.GetAsync("/staff/parcels/" + found);
        Assert.Contains(anaCode, parcelPage, StringComparison.Ordinal);
        (HttpResponseMessage _, string open) = await owner.GetAsync("/staff/tickets");
        Assert.DoesNotContain("/staff/tickets/" + ticketId, open, StringComparison.Ordinal);
        (HttpResponseMessage _, string all) = await owner.GetAsync("/staff/tickets?status=resolved");
        Assert.Contains("/staff/tickets/" + ticketId, all, StringComparison.Ordinal);
    }

    // Slice 5A: the shipments desk lists the customer's shipment, shows its parcels, gives both documents and changes the service.
    [SqlServerFact]
    public async Task ShipmentsDesk_ListsAndShowsAShipment_GivesItsDocuments_AndChangesItsService()
    {
        Guid tenant = NewTenant("deskco", "Desk Co");
        using CookieClient owner = await OwnerAsync("deskco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        await PreAlertAsync(tenant, ana, "1Z999AA10123456700", "Two pairs of shoes");

        // The company prices ground and air standard for everyone.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            foreach (string service in new[] { "ground", "air_standard" })
            {
                context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, service, null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                    new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                        Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, DateTimeOffset.UtcNow));
            }

            await context.SaveChangesAsync();
        }

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        Guid parcel = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456700")).GetProperty("parcelId").GetGuid();

        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/shipments");
        Assert.Contains(anaCode, list, StringComparison.Ordinal);
        Assert.Contains("0 expected, 1 received, 0 labelled", list, StringComparison.Ordinal);
        // The view chips (front-end markup since 2026-10-21): the name, then its count.
        Assert.Matches("To send(</span><span class=\"ops-status-chip__count\">| \\()1", list);
        string shipmentPath = Regex.Match(list, "href=\"(?<path>/staff/shipments/[0-9a-f-]{36})\"").Groups["path"].Value;
        Assert.NotEmpty(shipmentPath);
        (HttpResponseMessage _, string found) = await owner.GetAsync("/staff/shipments?search=" + anaCode.ToLowerInvariant());
        Assert.Contains(shipmentPath, found, StringComparison.Ordinal);
        (HttpResponseMessage _, string other) = await owner.GetAsync("/staff/shipments?search=C00000");
        Assert.DoesNotContain(shipmentPath, other, StringComparison.Ordinal);
        (HttpResponseMessage export, string csv) = await owner.GetAsync("/staff/shipments/export");
        Assert.Equal("text/csv", export.Content.Headers.ContentType?.MediaType);
        Assert.Contains(anaCode, csv, StringComparison.Ordinal);

        (HttpResponseMessage _, string page) = await owner.GetAsync(shipmentPath);
        Assert.Contains("/staff/parcels/" + parcel, page, StringComparison.Ordinal);
        Assert.Contains("Two pairs of shoes", page, StringComparison.Ordinal);
        Assert.Contains("Ana Pierre", page, StringComparison.Ordinal);
        Assert.Contains("value=\"ground\"", page, StringComparison.Ordinal);

        // Both documents as PDF; the first one numbers the shipment.
        foreach (string kind in new[] { "packing-list", "commercial-invoice" })
        {
            (HttpResponseMessage pdf, string body) = await owner.GetRawAsync(shipmentPath + "/documents/" + kind);
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.StartsWith("%PDF", body, StringComparison.Ordinal);
            Assert.StartsWith(kind + "-SH", pdf.Content.Headers.ContentDisposition?.FileNameStar ?? pdf.Content.Headers.ContentDisposition?.FileName?.Trim('"'), StringComparison.Ordinal);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(shipmentPath + "/documents/receipt")).Response.StatusCode);
        (HttpResponseMessage _, string numbered) = await owner.GetAsync("/staff/shipments");
        Assert.DoesNotContain("Not numbered yet", numbered, StringComparison.Ordinal);

        // The service: one the company prices, changed while the parcel is at the origin; one it does not price is refused.
        (HttpResponseMessage changed, string _) = await owner.PostAsync(shipmentPath + "/service", page, ("service", "ground"));
        Assert.Equal(shipmentPath + "?done=service", changed.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string afterChange) = await owner.GetAsync(shipmentPath + "?done=service");
        Assert.Contains("The service is changed.", afterChange, StringComparison.Ordinal);
        (HttpResponseMessage _, string refused) = await owner.PostAsync(shipmentPath + "/service", page, ("service", "ocean_full_container"));
        Assert.DoesNotContain("The service is changed.", refused, StringComparison.Ordinal);
        (HttpResponseMessage _, string byService) = await owner.GetAsync("/staff/shipments?service=ground");
        Assert.Contains(shipmentPath, byService, StringComparison.Ordinal);
    }

    // The Development demo day (owner, 2026-10-19): it goes through the real rules without a refusal, every stage shows, and the second company sees none of it.
    [SqlServerFact]
    public async Task DemoDay_FillsTheMainCompany_EveryStageShows_AndTheSecondCompanySeesNoneOfIt()
    {
        Guid main = NewTenant("bluewater-shipping", "Bluewater Shipping");
        Guid second = NewTenant("andes-courier", "Andes Courier");
        using CookieClient owner = await OwnerAsync("bluewater-shipping");
        using CookieClient other = await OwnerAsync("andes-courier");
        IOptions<OperationsDemoDataOptions> options = Options.Create(new OperationsDemoDataOptions());

        foreach ((Guid tenant, string slug, bool first) in new[] { (main, "bluewater-shipping", true), (second, "andes-courier", false) })
        {
            await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
                await ActivatorUtilities.CreateInstance<OperationsDemoStaffSeeder>(scope.ServiceProvider, options).SeedAsync(tenant, slug, CancellationToken.None);
            }

            OperationsDemoScenario scenario = ActivatorUtilities.CreateInstance<OperationsDemoScenario>(_host.Services, options);
            Assert.True(await scenario.SeedAsync(tenant, slug, first, CancellationToken.None));
            Assert.False(await scenario.SeedAsync(tenant, slug, first, CancellationToken.None));
        }

        (HttpResponseMessage _, string shipments) = await owner.GetAsync("/staff/shipments");
        Assert.Contains("Les Cayes", shipments, StringComparison.Ordinal);
        Assert.Contains("Carlos Rojas", shipments, StringComparison.Ordinal);
        (HttpResponseMessage _, string incoming) = await owner.GetAsync("/staff/parcels/incoming?view=overdue");
        Assert.Contains("771234567890", incoming, StringComparison.Ordinal);
        (HttpResponseMessage _, string undeclared) = await owner.GetAsync("/staff/parcels/unidentified");
        Assert.Contains("1Z999AA10123454421", undeclared, StringComparison.Ordinal);
        (HttpResponseMessage _, string tickets) = await owner.GetAsync("/staff/tickets");
        Assert.Contains("1ZW45E670323454421", tickets, StringComparison.Ordinal);
        async Task<string> NumberAsync(string tracking)
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(main);
            return (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.SingleAsync(item => item.CarrierTrackingNumber == tracking)).Number!;
        }

        (HttpResponseMessage _, string holds) = await owner.GetAsync("/staff/holds");
        Assert.Contains(await NumberAsync("789900112277"), holds, StringComparison.Ordinal);
        Assert.Contains(await NumberAsync("TBA302775533110"), holds, StringComparison.Ordinal);
        (HttpResponseMessage _, string labels) = await owner.GetAsync("/staff/parcels/labels");
        Assert.Contains(await NumberAsync("1Z999AA10123456701"), labels, StringComparison.Ordinal);
        Assert.DoesNotContain(await NumberAsync("TBA301884422190"), labels, StringComparison.Ordinal);
        (HttpResponseMessage _, string queue) = await owner.GetAsync("/staff/inspection/queue");
        Assert.Contains(await NumberAsync("771234567891"), queue, StringComparison.Ordinal);

        // The second company: its own shipment, none of the first company's.
        (HttpResponseMessage _, string theirs) = await other.GetAsync("/staff/shipments");
        Assert.Contains("Rosa Quispe", theirs, StringComparison.Ordinal);
        Assert.DoesNotContain("Carlos Rojas", theirs, StringComparison.Ordinal);
        (HttpResponseMessage _, string search) = await other.GetAsync("/staff/parcels?search=TBA301884422190");
        Assert.DoesNotContain("TBA301884422190", search.Replace("value=\"TBA301884422190\"", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    // Slice 5B step 1: the company records its policies with their documents, puts its cover under one, sets its liability, and an insured shipment has its certificate.
    [SqlServerFact]
    public async Task Insurance_RecordsPolicies_PutsTheCoverUnderOne_SetsLiability_AndCertifiesInsuredShipments()
    {
        Guid tenant = NewTenant("insureco", "Insure Co");
        using CookieClient owner = await OwnerAsync("insureco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana@example.com");

        (HttpResponseMessage _, string empty) = await owner.GetAsync("/staff/insurance");
        Assert.Contains("No policy recorded yet.", empty, StringComparison.Ordinal);

        // A cargo policy for customers' shipments; a liability kind for it is refused.
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/insurance/policies/new");
        (string, string)[] fields =
        {
            ("scope", "CustomerShipments"), ("kind", "OpenCargo"), ("insurer", "Atlantic Marine"), ("number", "OC-2026-1"), ("starts", "2026-01-01"),
            ("ends", DateTime.UtcNow.AddDays(20).ToString("yyyy-MM-dd")), ("currency", "USD"), ("limit_shipment", "25000"), ("deductible_amount", "250"), ("risks", "Loss"),
            ("risks", "Damage"), ("modes", "Air"), ("exclusions", "CashAndDocuments"), ("notice_days", "30"), ("declarations", "Monthly")
        };
        (HttpResponseMessage _, string wrongKind) = await owner.PostAsync("/staff/insurance/policies/new", form, fields.Select(field => field.Item1 == "kind" ? ("kind", "WarehouseLegalLiability") : field).ToArray());
        Assert.Contains("That kind of policy does not fit what it covers.", wrongKind, StringComparison.Ordinal);
        Assert.Contains("value=\"Atlantic Marine\"", wrongKind, StringComparison.Ordinal);
        (HttpResponseMessage saved, string _) = await owner.PostAsync("/staff/insurance/policies/new", form, fields);
        string policyPath = saved.Headers.Location!.OriginalString.Split('?')[0];
        Assert.Matches("^/staff/insurance/policies/[0-9a-f-]{36}$", policyPath);

        // Its document: a text file refused, a PDF kept and served as a sandboxed download.
        (HttpResponseMessage _, string policyPage) = await owner.GetAsync(policyPath);
        (HttpResponseMessage _, string refusedFile) = await owner.PostFormWithFileAsync(policyPath + "/documents", policyPage, "document", "policy.txt", "text/plain", "plain"u8.ToArray(), ("kind", "policy"));
        Assert.Contains("The document must be a PDF, PNG or JPEG file of up to 10 MB.", refusedFile, StringComparison.Ordinal);
        byte[] pdf = "%PDF-1.4\n%\u00e2\u00e3\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF"u8.ToArray();
        (HttpResponseMessage filed, string _) = await owner.PostFormWithFileAsync(policyPath + "/documents", policyPage, "document", "OC-2026-1.pdf", "application/pdf", pdf, ("kind", "policy"));
        Assert.Equal(policyPath + "?done=document", filed.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string withDocument) = await owner.GetAsync(policyPath);
        string documentPath = Regex.Match(withDocument, "href=\"(?<path>" + Regex.Escape(policyPath) + "/documents/[0-9a-f-]{36})\"").Groups["path"].Value;
        (HttpResponseMessage download, string _) = await owner.GetRawAsync(documentPath);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);

        // The list flags it: it ends within 30 days.
        (HttpResponseMessage _, string overview) = await owner.GetAsync("/staff/insurance");
        Assert.Contains("Atlantic Marine OC-2026-1 ends on", overview, StringComparison.Ordinal);

        // The cover sold by air sits under it; the liability when not insured.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            context.Add(Vixly.Operations.Domain.Charges.OperationsInsurancePlan.Create(tenant, Vixly.Operations.Domain.Charges.OperationsTransportMode.Air,
                new Vixly.Operations.Domain.Charges.OperationsInsuranceTerms(Vixly.Operations.Domain.Charges.OperationsInsuranceMethod.Percent, 3m, 5m,
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsuranceBand>(), 5000m), null, DateTimeOffset.UtcNow.AddDays(-1)));
            await context.SaveChangesAsync();
        }

        Guid policyId = Guid.Parse(policyPath[(policyPath.LastIndexOf('/') + 1)..]);
        (HttpResponseMessage _, string covers) = await owner.GetAsync("/staff/insurance");
        Assert.Equal("/staff/insurance?done=cover", (await owner.PostAsync("/staff/insurance/covers/Air", covers, ("policy", policyId.ToString()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string badLiability) = await owner.PostAsync("/staff/insurance/liability/Air", covers, ("basis", "PerWeight"), ("amount", ""), ("cap", "100"));
        Assert.Contains("Give the amount for", badLiability, StringComparison.Ordinal);
        Assert.Equal("/staff/insurance?done=liability", (await owner.PostAsync("/staff/insurance/liability/Air", covers, ("basis", "PerWeight"), ("amount", "9.07"), ("cap", "100"),
            ("customer_text", "Up to $9.07 per lb."))).Response.Headers.Location?.OriginalString);

        // A policy for the company's operations cannot stand behind a cover sold to customers.
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/staff/insurance/policies/new", form, ("scope", "CompanyOperations"), ("kind", "FreightForwarderLiability"),
            ("insurer", "Gulfstream Mutual"), ("number", "FFL-1"), ("currency", "USD"))).Response.StatusCode);
        (HttpResponseMessage _, string withBoth) = await owner.GetAsync("/staff/insurance");
        Guid operations = Guid.Parse(Regex.Match(withBoth, "href=\"/staff/insurance/policies/(?<id>[0-9a-f-]{36})\">Gulfstream Mutual FFL-1").Groups["id"].Value);
        (HttpResponseMessage _, string scopeRefused) = await owner.PostAsync("/staff/insurance/covers/Air", withBoth, ("policy", operations.ToString()));
        Assert.Contains("A cover sold to customers sits only under a policy for customers", scopeRefused, StringComparison.Ordinal);

        // Ana insures a shipment: it has a certificate naming the policy, and staff open it from the shipment page.
        Guid shipmentId;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Domain.Shipments.OperationsShipment shipment = Vixly.Operations.Domain.Shipments.OperationsShipment.Request(tenant, ana, warehouse,
                Vixly.Operations.Domain.Shipments.OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"),
                    "Ana Pierre", "+50937123456", Vixly.Operations.Domain.Shipments.OperationsShipmentOptions.Insured).ForService("air_standard").AcceptTerms(), DateTimeOffset.UtcNow);
            scope.ServiceProvider.GetRequiredService<IOperationsReceivingRepository>().Add(shipment);
            scope.ServiceProvider.GetRequiredService<IOperationsReceivingRepository>().Add(OperationsPreAlert.Create(shipment, "UPS", "1Z999AA10123450011", "Shoe store", "Shoes", 300m, "USD", null, DateTimeOffset.UtcNow));
            await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
            shipmentId = shipment.Id;
        }

        (HttpResponseMessage certificate, string certificateBody) = await owner.GetRawAsync("/staff/shipments/" + shipmentId + "/certificate");
        Assert.Equal("application/pdf", certificate.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", certificateBody, StringComparison.Ordinal);
        Assert.StartsWith("insurance-certificate-IC", certificate.Content.Headers.ContentDisposition?.FileNameStar ?? certificate.Content.Headers.ContentDisposition?.FileName?.Trim('"'),
            StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Insurance.OperationsInsuranceCertificate issued = await context.InsuranceCertificates.SingleAsync(item => item.ShipmentId == shipmentId);
            Assert.Equal((policyId, Vixly.Operations.Domain.Insurance.OperationsCertificateStatus.Provisional), (issued.PolicyId!.Value, issued.Status));

            // Review: staff cannot move an insured shipment to another mode (the customer accepted the air terms).
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "ground", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        string insuredPath = "/staff/shipments/" + shipmentId;
        (HttpResponseMessage _, string insuredPage) = await owner.GetAsync(insuredPath);
        (HttpResponseMessage _, string modeRefused) = await owner.PostAsync(insuredPath + "/service", insuredPage, ("service", "ground"));
        Assert.Contains("The customer insured this shipment under the terms of its transport mode.", WebUtility.HtmlDecode(modeRefused), StringComparison.Ordinal);

        // Archived, the policy no longer stands behind the cover.
        (HttpResponseMessage _, string toArchive) = await owner.GetAsync(policyPath);
        Assert.Equal(policyPath + "?done=archived", (await owner.PostAsync(policyPath + "/archive", toArchive, ("archived", "true"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string after) = await owner.GetAsync("/staff/insurance");
        Assert.DoesNotContain("selected value=\"" + policyId + "\"", after, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Counter_FindsByPhone_CreatesTheShipment_AndTheLastParcelMakesItFinal()
    {
        Guid tenant = NewTenant("counterco", "Counter Co");
        using CookieClient owner = await OwnerAsync("counterco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana.counter@example.com");
        Guid destination;

        // The company has a pickup point in Port-au-Prince and prices air standard for everyone.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        // The counter opens on its search box.
        (HttpResponseMessage start, string startPage) = await owner.GetAsync("/staff/counter");
        Assert.True(start.StatusCode == HttpStatusCode.OK, startPage);
        Assert.Contains("name=\"find\"", startPage, StringComparison.Ordinal);

        // The clerk types the end of the phone number: the one customer opens.
        (HttpResponseMessage found, string _) = await owner.GetAsync("/staff/counter?find=37123456");
        Assert.Equal("/staff/counter/customers/" + ana, found.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string nobody) = await owner.GetAsync("/staff/counter?find=nobody@example.com");
        Assert.Contains("No customer has this phone number, email or code.", WebUtility.HtmlDecode(nobody), StringComparison.Ordinal);
        (HttpResponseMessage _, string byCode) = await owner.GetAsync("/staff/counter?find=" + anaCode);
        Assert.Equal("/staff/counter/customers/" + ana, (await owner.GetAsync("/staff/counter?find=" + anaCode)).Response.Headers.Location?.OriginalString);
        Assert.NotNull(byCode);

        // A new shipment of two parcels, picked up in Port-au-Prince, by air.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        Assert.Contains("ana.counter@example.com", customerPage, StringComparison.Ordinal);

        // Owner, 2026-10-20: the customer's card calls for attention, and the next customer is one click away.
        Assert.Contains("data-standing=\"good\"", customerPage, StringComparison.Ordinal);
        Assert.Contains("In good standing", customerPage, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/counter\" accesskey=\"n\"", customerPage, StringComparison.Ordinal);
        (HttpResponseMessage created, string createdHtml) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "2"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})\\?done=created$").Groups["path"].Value;
        Assert.True(shipmentPath.Length > 0, created.StatusCode + " " + Regex.Match(createdHtml, "role=\"alert\">\\s*<div[^>]*>(?<m>[\\s\\S]*?)</div>").Groups["m"].Value);
        Guid shipmentId = Guid.Parse(shipmentPath[(shipmentPath.LastIndexOf('/') + 1)..]);

        // A parcel without a description is refused, and what the clerk typed stays.
        (HttpResponseMessage _, string shipmentPage) = await owner.GetAsync(shipmentPath);
        Assert.Contains("0 of 2 parcels", WebUtility.HtmlDecode(shipmentPage), StringComparison.Ordinal);
        (HttpResponseMessage noContents, string refused) = await owner.PostAsync(shipmentPath + "/parcels", shipmentPage, ("key", FormKey(shipmentPage, shipmentPath + "/parcels")),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", " "), ("value", "40"));
        Assert.Null(noContents.Headers.Location);
        Assert.Contains("value=\"2.5\"", refused, StringComparison.Ordinal);

        // The first parcel: entered, a label to print; the count can still be corrected only before the first parcel.
        (HttpResponseMessage first, string _) = await owner.PostAsync(shipmentPath + "/parcels", refused, ("key", FormKey(refused, shipmentPath + "/parcels")),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Clothes"), ("value", "40"));
        Assert.Equal(shipmentPath + "?done=added", first.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string afterFirst) = await owner.GetAsync(shipmentPath + "?done=added");
        Assert.Contains("1 of 2 parcels", WebUtility.HtmlDecode(afterFirst), StringComparison.Ordinal);
        Assert.Contains("href=\"" + customerPath + "\" accesskey=\"x\"", afterFirst, StringComparison.Ordinal);
        Assert.Matches("/staff/parcels/[0-9a-f-]{36}/label", afterFirst);
        (HttpResponseMessage _, string countLocked) = await owner.PostAsync(shipmentPath + "/count", afterFirst, ("parcelCount", "3"));
        Assert.Contains("The count can only be corrected on a shipment made at the counter, before its first parcel.", WebUtility.HtmlDecode(countLocked), StringComparison.Ordinal);

        // The second and last parcel: the shipment is final, with its amount and a booked event on each parcel.
        (HttpResponseMessage last, string _) = await owner.PostAsync(shipmentPath + "/parcels", afterFirst, ("key", FormKey(afterFirst, shipmentPath + "/parcels")),
            ("weight", "1,5"), ("length", "20"), ("width", "20"), ("height", "10"), ("contents", "Shoes"), ("value", "60"));
        Assert.Equal(shipmentPath + "?done=booked", last.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string booked) = await owner.GetAsync(shipmentPath + "?done=booked");
        Assert.Contains("The last parcel is in: the shipment is final.", WebUtility.HtmlDecode(booked), StringComparison.Ordinal);
        Assert.DoesNotContain(shipmentPath + "/parcels\"", booked, StringComparison.Ordinal);

        // More boxes need a new shipment.
        (HttpResponseMessage _, string third) = await owner.PostAsync(shipmentPath + "/parcels", afterFirst, ("key", Guid.NewGuid().ToString("N")),
            ("weight", "1"), ("length", "10"), ("width", "10"), ("height", "10"), ("contents", "Toys"), ("value", "10"));
        Assert.Contains("This shipment is final: it can no longer change.", WebUtility.HtmlDecode(third), StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsShipment shipment = await context.Shipments.SingleAsync(item => item.Id == shipmentId);
            Assert.Equal((OperationsShipmentStatus.Booked, 2, true, warehouse), (shipment.Status, shipment.ParcelCount!.Value, shipment.CreatedAtCounter, shipment.OriginWarehouseId));
            Assert.True(shipment.BookedTotal is > 0m);
            Assert.NotNull(shipment.BookedUtc);
            List<Guid> parcels = await context.Parcels.Where(item => item.ShipmentId == shipmentId).Select(item => item.Id).ToListAsync();
            Assert.Equal(2, parcels.Count);
            Assert.Equal(2, await context.TrackingEvents.CountAsync(item => parcels.Contains(item.SubjectId) && item.Code == OperationsTrackingCodes.Booked));
            Assert.Equal(2, await context.TrackingEvents.CountAsync(item => parcels.Contains(item.SubjectId) && item.Source == OperationsTrackingSource.Counter
                && item.Code == OperationsTrackingCodes.ReceivedAtOrigin));
        }

        // The customer's shipments at the counter show it final.
        (HttpResponseMessage _, string listed) = await owner.GetAsync(customerPath);
        Assert.Contains(shipmentPath, listed, StringComparison.Ordinal);
        Assert.Contains("Final", listed, StringComparison.Ordinal);

        // Not in good standing (owner, 2026-10-20): the card says why, and no new shipment is taken.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsCustomer suspended = await context.Customers.SingleAsync(item => item.Id == ana);
            suspended.ChangeStatus(OperationsCustomerStatus.Suspended, "Unpaid invoices", DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        (HttpResponseMessage _, string refusedCard) = await owner.GetAsync(customerPath);
        Assert.Contains("data-standing=\"bad\"", refusedCard, StringComparison.Ordinal);
        Assert.Contains("The account is suspended: no new shipment or parcel until it is reactivated.", WebUtility.HtmlDecode(refusedCard), StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"" + customerPath + "/shipments\"", refusedCard, StringComparison.Ordinal);
        (HttpResponseMessage _, string stillRefused) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", Guid.NewGuid().ToString("N")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        Assert.Contains("This customer’s account is not active.", WebUtility.HtmlDecode(stillRefused), StringComparison.Ordinal);
    }

    // Slice 5C (C-2, C-4): at the dock, the station check of the last declared parcel makes a portal shipment final; more parcels than declared wait for the customer.
    [SqlServerFact]
    public async Task StationCheck_OfTheLastDeclaredParcel_MakesTheShipmentFinal_AndMoreThanDeclaredWaits()
    {
        Guid tenant = NewTenant("dockbook", "Dock Book");
        using CookieClient owner = await OwnerAsync("dockbook");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.dock@example.com");
        Guid printer;
        Guid exact;
        Guid over;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Vixly.Operations.Domain.Printing.OperationsLabelPrinter shipping = Vixly.Operations.Domain.Printing.OperationsLabelPrinter.Create(tenant, warehouse, "Inspection 1",
                Vixly.Operations.Domain.Printing.OperationsPrinterConnection.Network, "192.168.1.60:9100", 203, now, Vixly.Operations.Domain.Printing.OperationsLabelMedia.ShippingLabel);
            context.Add(shipping);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));

            // One shipment declares one box and one comes; the other declares one box and two come.
            OperationsShipment one = Portal(tenant, ana, warehouse, now);
            OperationsShipment more = Portal(tenant, ana, warehouse, now);
            context.AddRange(one, more);
            context.Add(Vixly.Operations.Domain.Receiving.OperationsPreAlert.Create(one, "UPS", "1Z999AA10123457001", "Shoe store", "Shoes", 80m, "USD", null, now));
            context.Add(Vixly.Operations.Domain.Receiving.OperationsPreAlert.Create(more, "UPS", "1Z999AA10123457002", "Shoe store", "Shoes", 80m, "USD", null, now));
            context.Add(Vixly.Operations.Domain.Receiving.OperationsPreAlert.Create(more, "UPS", "1Z999AA10123457003", "Shoe store", "Socks", 20m, "USD", null, now));
            await context.SaveChangesAsync();
            (printer, exact, over) = (shipping.Id, one.Id, more.Id);
        }

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        foreach (string tracking in new[] { "1Z999AA10123457001", "1Z999AA10123457002", "1Z999AA10123457003" })
        {
            Guid parcel = (await ScanOkAsync(owner, station, delivery, tracking)).GetProperty("parcelId").GetGuid();
            (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/inspection/" + parcel);
            (HttpResponseMessage checkedAt, string _) = await StationCheckAsync(owner, parcel, form, printer);
            Assert.StartsWith("/staff/inspection?done=", checkedAt.Headers.Location?.OriginalString, StringComparison.Ordinal);
        }

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsShipment booked = await context.Shipments.SingleAsync(item => item.Id == exact);
            Assert.Equal(OperationsShipmentStatus.Booked, booked.Status);
            Assert.True(booked.BookedTotal is > 0m);
            Assert.Equal(1, await context.TrackingEvents.CountAsync(item => item.Code == OperationsTrackingCodes.Booked && item.Source == OperationsTrackingSource.Scan
                && context.Parcels.Any(parcel => parcel.Id == item.SubjectId && parcel.ShipmentId == exact)));
            Assert.Equal(OperationsShipmentStatus.Open, (await context.Shipments.SingleAsync(item => item.Id == over)).Status);
        }

        // The counter shows the second one as more parcels than declared.
        (HttpResponseMessage _, string customer) = await owner.GetAsync("/staff/counter/customers/" + ana);
        Assert.Contains("More parcels than declared", customer, StringComparison.Ordinal);
    }

    private static OperationsShipment Portal(Guid tenant, Guid customer, Guid warehouse, DateTimeOffset now)
    {
        OperationsShipment shipment = OperationsShipment.Request(tenant, customer, warehouse,
            OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), "Ana Pierre", "+50937123456",
                OperationsShipmentOptions.None).ForService("air_standard"), now);
        shipment.DeclareParcelCount(1, 0, now);
        return shipment;
    }

    // Slice 5D (owner, 2026-10-20): two insurance products side by side at the counter, with their price for the shipment; the customer accepts, the clerk records it.
    [SqlServerFact]
    public async Task Counter_OffersEachInsuranceProduct_WithItsPrice_AndRecordsTheCustomersChoice()
    {
        Guid tenant = NewTenant("insureco", "Insure Co");
        using CookieClient owner = await OwnerAsync("insureco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.insure@example.com");
        Guid destination;
        Guid basic;
        Guid full;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            Vixly.Operations.Domain.Charges.OperationsInsurancePlan cheap = Vixly.Operations.Domain.Charges.OperationsInsurancePlan.Create(tenant, Vixly.Operations.Domain.Charges.OperationsTransportMode.Air,
                new Vixly.Operations.Domain.Charges.OperationsInsuranceTerms(Vixly.Operations.Domain.Charges.OperationsInsuranceMethod.Percent, 2m, 5m,
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsuranceBand>(), 1000m, null), null, now, "Basic");
            Vixly.Operations.Domain.Charges.OperationsInsurancePlan rich = Vixly.Operations.Domain.Charges.OperationsInsurancePlan.Create(tenant, Vixly.Operations.Domain.Charges.OperationsTransportMode.Air,
                new Vixly.Operations.Domain.Charges.OperationsInsuranceTerms(Vixly.Operations.Domain.Charges.OperationsInsuranceMethod.Percent, 5m, 10m,
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsuranceBand>(), null, null), null, now, "Full value");
            rich.Describe("Full value", "Everything you declared, no deductible.", null, null, 1, now);
            context.AddRange(cheap, rich);
            await context.SaveChangesAsync();
            (destination, basic, full) = (pickup.Id, cheap.Id, rich.Id);
        }

        // The new shipment lists both products with their rule and pitch; insured without a choice is refused.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        Assert.Contains("value=\"" + basic + "\"", customerPage, StringComparison.Ordinal);
        Assert.Contains("Full value", customerPage, StringComparison.Ordinal);
        Assert.Contains("Everything you declared, no deductible.", customerPage, StringComparison.Ordinal);
        Assert.Contains("2% of the declared value, at least 5.00 USD", WebUtility.HtmlDecode(customerPage), StringComparison.Ordinal);
        (string, string)[] fields = { ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "2"),
            ("recipient", "Jean Baptiste"), ("recipientPhone", "+50937000000") };

        // Slice 5D: the estimate beside the new shipment, before any parcel: the rates, and Full value on the 400 USD the customer expects to declare (5% = 20.00).
        Assert.Contains("data-estimate=\"" + customerPath + "/estimate\"", customerPage, StringComparison.Ordinal);
        string early = WebUtility.HtmlDecode((await owner.PostAsync(customerPath + "/estimate", customerPage,
            fields.Append(("insurance", full.ToString())).Append(("expectedValue", "400")).ToArray())).Html);
        Assert.Contains("Shipment estimate", early, StringComparison.Ordinal);
        Assert.Contains("from 0: 3.00 USD / kg", early, StringComparison.Ordinal);
        Assert.Contains("20.00 USD", early, StringComparison.Ordinal);
        Assert.Contains("With Basic", early, StringComparison.Ordinal);
        Assert.DoesNotContain("<html", early, StringComparison.Ordinal);

        // Review of 5D: 5 000 USD is above Basic's maximum (1 000): Basic is not possible, never its minimum premium; Full value is 250.00 (5%).
        string tooMuch = WebUtility.HtmlDecode((await owner.PostAsync(customerPath + "/estimate", customerPage,
            fields.Append(("insurance", basic.ToString())).Append(("expectedValue", "5,000")).ToArray())).Html);
        Assert.Contains("not possible for this value", tooMuch, StringComparison.Ordinal);
        Assert.Contains("250.00 USD", tooMuch, StringComparison.Ordinal);
        (HttpResponseMessage _, string notAccepted) = await owner.PostAsync(customerPath + "/shipments", customerPage,
            fields.Append(("key", Guid.NewGuid().ToString("N"))).Append(("insurance", full.ToString())).ToArray());
        Assert.Contains("Accept the insurance terms to insure the shipment.", WebUtility.HtmlDecode(notAccepted), StringComparison.Ordinal);
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage,
            fields.Append(("key", Guid.NewGuid().ToString("N"))).Append(("insurance", full.ToString())).Append(("insuranceAccepted", "true")).ToArray());
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        Assert.NotEmpty(shipmentPath);
        Guid shipmentId = Guid.Parse(shipmentPath[(shipmentPath.LastIndexOf('/') + 1)..]);

        // Review of 5D: the page to sign works from the start, before any parcel.
        (HttpResponseMessage first, string firstBody) = await owner.GetRawAsync(shipmentPath + "/declaration");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.StartsWith("%PDF", firstBody, StringComparison.Ordinal);

        // A parcel of 400 USD: Basic costs 8.00 (2%), Full value 20.00 (5%); the freight alone is 9.00 (2.5 kg billed as 3 kg x 3).
        (HttpResponseMessage _, string shipmentPage) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/parcels", shipmentPage, ("key", FormKey(shipmentPage, shipmentPath + "/parcels")),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Laptop"), ("value", "400"));
        (HttpResponseMessage _, string withParcel) = await owner.GetAsync(shipmentPath);
        string shown = WebUtility.HtmlDecode(withParcel);
        Assert.Contains("+8.00 USD · total 17.00 USD", shown, StringComparison.Ordinal);
        Assert.Contains("+20.00 USD · total 29.00 USD", shown, StringComparison.Ordinal);

        // The estimate beside the shipment: the parcel by its number and size, the insurance, the total, and what if.
        Assert.Contains("id=\"shipment-estimate\"", withParcel, StringComparison.Ordinal);
        Assert.Matches("PK\\d+\\s*<small>2.5 kg · 30 × 20 × 10 cm · billed 3 kg</small>", shown);
        Assert.Contains("With Basic", shown, StringComparison.Ordinal);

        // The customer takes Basic instead; without their acceptance it is refused.
        (HttpResponseMessage _, string unaccepted) = await owner.PostAsync(shipmentPath + "/insurance", withParcel, ("insurance", basic.ToString()));
        Assert.Contains("Accept the insurance terms to insure the shipment.", WebUtility.HtmlDecode(unaccepted), StringComparison.Ordinal);
        Assert.Equal(shipmentPath + "?done=insurance",
            (await owner.PostAsync(shipmentPath + "/insurance", withParcel, ("insurance", basic.ToString()), ("insuranceAccepted", "true"))).Response.Headers.Location?.OriginalString);

        Guid clerk;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsShipment shipment = await context.Shipments.SingleAsync(item => item.Id == shipmentId);
            Assert.Equal(basic, shipment.InsurancePlanId);
            Assert.True(shipment.Options.HasFlag(OperationsShipmentOptions.Insured));
            Assert.NotNull(shipment.InsuranceAcceptedUtc);
            Assert.NotNull(shipment.InsuranceRecordedByUserId);
            clerk = shipment.InsuranceRecordedByUserId!.Value;
            Assert.Equal(basic, (await context.InsuranceCertificates.SingleAsync(item => item.ShipmentId == shipmentId && item.Status != Vixly.Operations.Domain.Insurance.OperationsCertificateStatus.Void)).PlanId);
            Assert.NotEqual(Guid.Empty, clerk);
        }

        // Slice 5D (D-8): the page to sign is a PDF; a signature on screen is kept with the shipment; anything but a PNG is refused.
        (HttpResponseMessage declaration, string declarationBody) = await owner.GetRawAsync(shipmentPath + "/declaration");
        Assert.Equal("application/pdf", declaration.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", declarationBody, StringComparison.Ordinal);
        (HttpResponseMessage _, string toSign) = await owner.GetAsync(shipmentPath);
        Assert.Contains("data-signature-pad", toSign, StringComparison.Ordinal);
        Assert.Contains("The signature could not be read.", WebUtility.HtmlDecode((await owner.PostAsync(shipmentPath + "/signature", toSign, ("signature", "data:image/png;base64,AAAA"))).Html),
            StringComparison.Ordinal);
        const string pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        Assert.Equal(shipmentPath + "?done=signed", (await owner.PostAsync(shipmentPath + "/signature", toSign, ("signature", "data:image/png;base64," + pixel))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Signed on screen on", (await owner.GetAsync(shipmentPath)).Html, StringComparison.Ordinal);
        Assert.StartsWith("%PDF", (await owner.GetRawAsync(shipmentPath + "/declaration")).Item2, StringComparison.Ordinal);

        // Review of 5D: another insurance makes the signature stale; the counter asks for it again.
        (HttpResponseMessage _, string signedPage) = await owner.GetAsync(shipmentPath);
        Assert.Equal(shipmentPath + "?done=insurance",
            (await owner.PostAsync(shipmentPath + "/insurance", signedPage, ("insurance", full.ToString()), ("insuranceAccepted", "true"))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Signed on screen on", (await owner.GetAsync(shipmentPath)).Html, StringComparison.Ordinal);

        // The company decides staff may not change it: the choice is no longer offered and the server refuses it.
        (HttpResponseMessage _, string insurancePage) = await owner.GetAsync("/staff/insurance");
        Assert.Equal("/staff/insurance?done=settings", (await owner.PostAsync("/staff/insurance/settings", insurancePage, ("staff_may_change", "false"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string locked) = await owner.GetAsync(shipmentPath);
        Assert.DoesNotContain("action=\"" + shipmentPath + "/insurance\"", locked, StringComparison.Ordinal);
        Assert.DoesNotContain("done=insurance", (await owner.PostAsync(shipmentPath + "/insurance", withParcel, ("insurance", ""), ("insuranceAccepted", "true"))).Response.Headers.Location?.OriginalString ?? string.Empty,
            StringComparison.Ordinal);
    }

    // Slice 5E step 3: supplies at the counter, on the shipment being processed and ordered on their own.
    [SqlServerFact]
    public async Task Counter_SellsSupplies_OnTheShipmentUntilFinal_AndOnTheirOwn_WithDiscountsByPolicy()
    {
        Guid tenant = NewTenant("supplydesk", "Supply Desk");
        (CookieClient owner, string ownerKey) = await OwnerWithKeyAsync("supplydesk");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.supplies@example.com");
        Guid destination;
        (Guid tape, Guid boxM, Guid boxL) = (Guid.Empty, Guid.Empty, Guid.Empty);

        // A pickup point, air standard for everyone, and three supplies: tape (barcode 0123, 5 in stock), a medium box (10 or more at 2.00) and a large box.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            Vixly.Operations.Domain.Supplies.OperationsSupply Supply(string code, string name, decimal price, string[] barcodes, Vixly.Operations.Domain.Supplies.OperationsSupplyBreak[]? breaks) =>
                Vixly.Operations.Domain.Supplies.OperationsSupply.Create(tenant, new Vixly.Operations.Domain.Supplies.OperationsSupplyDetails(code, name, null, null, null,
                    Vixly.Operations.Domain.Supplies.OperationsSupplyCategory.Boxes, true), barcodes, Vixly.Operations.Domain.Supplies.OperationsSupplyPrice.Create(price, breaks, null, now), now);
            Vixly.Operations.Domain.Supplies.OperationsSupply t = Supply("TAPE", "Packing tape", 4.50m, new[] { "0123" }, null);
            Vixly.Operations.Domain.Supplies.OperationsSupply m = Supply("BOX-M", "Medium box", 2.50m, Array.Empty<string>(), new[] { new Vixly.Operations.Domain.Supplies.OperationsSupplyBreak(10, 2.00m) });
            Vixly.Operations.Domain.Supplies.OperationsSupply l = Supply("BOX-L", "Large box", 3.75m, Array.Empty<string>(), null);
            context.AddRange(t, m, l);
            Vixly.Operations.Domain.Supplies.OperationsSupplyStock stock = Vixly.Operations.Domain.Supplies.OperationsSupplyStock.Create(tenant, t.Id, warehouse, now);
            Vixly.Operations.Domain.Supplies.OperationsSupplyMovement received = Vixly.Operations.Domain.Supplies.OperationsSupplyMovement.Received(tenant, t.Id, warehouse, 5, null, null, null, Guid.NewGuid(), Guid.NewGuid(), now);
            stock.Apply(received, now);
            context.AddRange(stock, received);
            await context.SaveChangesAsync();
            (destination, tape, boxM, boxL) = (pickup.Id, t.Id, m.Id, l.Id);
        }

        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        Assert.NotEmpty(shipmentPath);

        // A scan of the barcode adds one tape; the item code scanned again adds another; the same form twice adds once (ADR-0019).
        (HttpResponseMessage _, string page) = await owner.GetAsync(shipmentPath);
        Assert.Contains("data-supplies", page, StringComparison.Ordinal);
        Assert.Contains("only be added before the last parcel", WebUtility.HtmlDecode(page), StringComparison.Ordinal);
        string key = Key();
        Assert.Equal(shipmentPath + "?done=supply", (await owner.PostAsync(shipmentPath + "/supplies", page, ("key", key), ("scan", "0123"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(shipmentPath + "?done=supply", (await owner.PostAsync(shipmentPath + "/supplies", page, ("key", key), ("scan", "0123"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(shipmentPath + "?done=supply", (await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("scan", "tape"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("No supply in use matches", WebUtility.HtmlDecode((await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("scan", "zebra"))).Html), StringComparison.Ordinal);

        // A name matching two boxes lists them; the clerk picks the large one.
        string choices = WebUtility.HtmlDecode((await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("scan", "box"))).Html);
        Assert.Contains("Medium box", choices, StringComparison.Ordinal);
        Assert.Contains("Large box", choices, StringComparison.Ordinal);
        // None in stock: added with a warning (the company allows it by default, S-9).
        Assert.Equal(shipmentPath + "?done=supply_short", (await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("supply", boxL.ToString()))).Response.Headers.Location?.OriginalString);
        Assert.Contains("check the shelf", WebUtility.HtmlDecode((await owner.GetAsync(shipmentPath + "?done=supply_short")).Html), StringComparison.Ordinal);

        // The estimate counts them: 2 tapes 9.00, the large box 3.75.
        (HttpResponseMessage _, string withSupplies) = await owner.GetAsync(shipmentPath);
        string shown = WebUtility.HtmlDecode(withSupplies);
        Assert.Contains("2 × TAPE · Packing tape", shown, StringComparison.Ordinal);
        Assert.Contains("12.75 USD", shown, StringComparison.Ordinal);

        // Discounts by policy (10% per line by default): 5% with a reason is fine; 30% from a clerk needs an approval, a wrong one is refused.
        string tapeLine = Regex.Match(withSupplies, "action=\"(?<a>/staff/counter/sales/[0-9a-f-]{36}/lines/[0-9a-f-]{36})\"").Groups["a"].Value;
        Assert.NotEmpty(tapeLine);
        Assert.Contains("Give the reason for the discount.", WebUtility.HtmlDecode((await owner.PostAsync(tapeLine, withSupplies, ("key", Key()), ("shipment", ShipmentId(shipmentPath)),
            ("quantity", "2"), ("discount", "5"))).Html), StringComparison.Ordinal);
        using CookieClient clerk = await ClerkAsync(owner, "supplydesk");
        (HttpResponseMessage _, string clerkPage) = await clerk.GetAsync(shipmentPath);
        Assert.Equal(shipmentPath + "?done=line", (await clerk.PostAsync(tapeLine, clerkPage, ("key", Key()), ("shipment", ShipmentId(shipmentPath)), ("quantity", "2"), ("discount", "5"),
            ("reason", "Regular customer"))).Response.Headers.Location?.OriginalString);
        string needs = WebUtility.HtmlDecode((await clerk.PostAsync(tapeLine, clerkPage, ("key", Key()), ("shipment", ShipmentId(shipmentPath)), ("quantity", "2"),
            ("discount", "30"), ("reason", "Damaged roll"))).Html);
        Assert.Contains("above your limit", needs, StringComparison.Ordinal);
        string wrong = WebUtility.HtmlDecode((await clerk.PostAsync(tapeLine, clerkPage, ("key", Key()), ("shipment", ShipmentId(shipmentPath)), ("quantity", "2"),
            ("discount", "30"), ("reason", "Damaged roll"), ("approver_email", "grace@supplydesk.example"), ("approver_code", "123456"))).Html);
        Assert.Contains("The approval was not accepted.", wrong, StringComparison.Ordinal);
        Assert.Contains("A discount is from 0 to 99.99%", WebUtility.HtmlDecode((await owner.PostAsync(tapeLine, withSupplies, ("key", Key()), ("shipment", ShipmentId(shipmentPath)),
            ("quantity", "2"), ("discount", "100"), ("reason", "Free"))).Html), StringComparison.Ordinal);
        // The owner, who may approve discounts, types their email and the next code of their app on the clerk's screen.
        Assert.Equal(shipmentPath + "?done=line", (await clerk.PostAsync(tapeLine, clerkPage, ("key", Key()), ("shipment", ShipmentId(shipmentPath)), ("quantity", "2"),
            ("discount", "30"), ("reason", "Damaged roll"), ("approver_email", "grace@supplydesk.example"), ("approver_code", Code(ownerKey, DateTime.UtcNow.AddSeconds(30))))).Response.Headers.Location?.OriginalString);

        // The last parcel makes the shipment final: its supplies are sold from the stock; no more supplies on it.
        (HttpResponseMessage _, string beforeParcel) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/parcels", beforeParcel, ("key", FormKey(beforeParcel, shipmentPath + "/parcels")),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Clothes"), ("value", "40"));
        (HttpResponseMessage _, string final) = await owner.GetAsync(shipmentPath);
        Assert.DoesNotContain("name=\"scan\"", final, StringComparison.Ordinal);
        Assert.Contains("This shipment is final", WebUtility.HtmlDecode((await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("scan", "0123"))).Html), StringComparison.Ordinal);

        // Supplies on their own: an order, confirmed with its number and its stock; cancelled, the stock comes back.
        (HttpResponseMessage started, string _) = await owner.PostAsync(customerPath + "/orders", customerPage);
        string orderPath = started.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Matches("^/staff/counter/orders/[0-9a-f-]{36}$", orderPath);
        Assert.Equal(orderPath, (await owner.PostAsync(customerPath + "/orders", customerPage)).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string order) = await owner.GetAsync(orderPath);
        // Review of 5E: a name with a space is searched, never taken for a broken barcode.
        Assert.Equal(orderPath + "?done=supply", (await owner.PostAsync(orderPath + "/supplies", order, ("key", Key()), ("scan", "Packing tape"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(orderPath + "?done=confirmed", (await owner.PostAsync(orderPath + "/confirm", order)).Response.Headers.Location?.OriginalString);
        Assert.Matches("SO\\d{8}", (await owner.GetAsync(orderPath)).Html);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Supplies.OperationsSupplySale onShipment = await context.SupplySales.SingleAsync(sale => sale.ShipmentId != null);
            Assert.Equal(Vixly.Operations.Domain.Supplies.OperationsSupplySaleStatus.Confirmed, onShipment.Status);
            Vixly.Operations.Domain.Supplies.OperationsSupplySaleLine discounted = onShipment.Lines.Single(line => line.SupplyId == tape);
            Assert.Equal((30m, 6.30m), (discounted.DiscountPercent, discounted.Amount));
            Assert.NotNull(discounted.DiscountApprovedByUserId);
            Assert.Equal(2, (await context.SupplyStocks.SingleAsync(stock => stock.SupplyId == tape)).OnHand);
        }

        // Slice 5F (I-7): confirmed, the order has its invoice; it is cancelled by the void of that invoice, not here (the invoice test goes on from there).
        Assert.Contains("ask for the void of the invoice", WebUtility.HtmlDecode((await owner.PostAsync(orderPath + "/cancel", order)).Html), StringComparison.Ordinal);
        Assert.NotEqual(Guid.Empty, boxM);
    }

    // Slice 5F step 1: invoices issued by themselves at final and on a confirmed order, taxes by kind, credit notes and voids decided by another person.
    [SqlServerFact]
    public async Task Invoices_AreIssuedByThemselves_WithTaxesByKind_AndCorrectedOnlyWithAnotherPersonsApproval()
    {
        Guid tenant = NewTenant("invoiceco", "Invoice Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("invoiceco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana.invoices@example.com");
        Guid destination;
        Guid tape;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            Vixly.Operations.Domain.Supplies.OperationsSupply t = Vixly.Operations.Domain.Supplies.OperationsSupply.Create(tenant, new Vixly.Operations.Domain.Supplies.OperationsSupplyDetails("TAPE",
                "Packing tape", "Ruban adhésif", null, null, Vixly.Operations.Domain.Supplies.OperationsSupplyCategory.Tape, true), new[] { "0123" },
                Vixly.Operations.Domain.Supplies.OperationsSupplyPrice.Create(4.50m, null, null, now), now);
            Vixly.Operations.Domain.Supplies.OperationsSupplyStock stock = Vixly.Operations.Domain.Supplies.OperationsSupplyStock.Create(tenant, t.Id, warehouse, now);
            Vixly.Operations.Domain.Supplies.OperationsSupplyMovement received = Vixly.Operations.Domain.Supplies.OperationsSupplyMovement.Received(tenant, t.Id, warehouse, 5, null, null, null, Guid.NewGuid(), Guid.NewGuid(), now);
            stock.Apply(received, now);
            context.AddRange(t, stock, received);
            await context.SaveChangesAsync();
            (destination, tape) = (pickup.Id, t.Id);
        }

        // Settings → Invoices: 7% on supplies, a line per kind, a note; a rate above 100% is refused.
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/invoices");
        Assert.Contains("A tax rate is a percentage from 0 to 100", WebUtility.HtmlDecode((await owner.PostAsync("/staff/settings/invoices", settingsPage, ("taxName", "Sales tax"),
            ("suppliesRate", "150"))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/settings/invoices?done=1", (await owner.PostAsync("/staff/settings/invoices", settingsPage, ("taxName", "Sales tax"), ("taxNumber", "TX-778"),
            ("freightRate", "0"), ("insuranceRate", "0"), ("suppliesRate", "7"), ("chargesRate", "0"), ("perKind", "true"), ("noteEn", "Pay at the counter."))).Response.Headers.Location?.OriginalString);

        // A shipment with two tapes becomes final at its parcel: its invoice is issued with it.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        (HttpResponseMessage _, string page) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("scan", "0123"));
        await owner.PostAsync(shipmentPath + "/supplies", page, ("key", Key()), ("scan", "0123"));
        (HttpResponseMessage _, string beforeParcel) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/parcels", beforeParcel, ("key", FormKey(beforeParcel, shipmentPath + "/parcels")),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Clothes"), ("value", "40"));
        string final = WebUtility.HtmlDecode((await owner.GetAsync(shipmentPath)).Html);
        string number = Regex.Match(final, "Invoice (?<n>INV\\d{8})").Groups["n"].Value;
        Assert.NotEmpty(number);
        Guid invoiceId;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = await context.Invoices.SingleAsync();
            Vixly.Operations.Domain.Shipments.OperationsShipment shipment = await context.Shipments.SingleAsync(item => item.Id == invoice.ShipmentId);
            Assert.Equal((number, Vixly.Operations.Domain.Billing.OperationsInvoiceKind.Shipment, warehouse, anaCode), (invoice.Number, invoice.Kind, invoice.WarehouseId, invoice.Party.CustomerCode));
            Assert.Equal(9.00m, invoice.Lines.Where(line => line.SupplyId == tape).Sum(line => line.Amount));
            // Only the supplies are taxed (7% of 9.00); the freight is what the shipment was booked at.
            Assert.Equal((0.63m, shipment.BookedTotal + 9.63m, "TX-778", "Pay at the counter."), (invoice.TaxTotal, invoice.Total, invoice.Party.TaxNumber, invoice.Note));
            invoiceId = invoice.Id;
        }

        // The list finds it by the customer's code; the invoice shows its lines and taxes; the PDF prints; the currency is now fixed.
        Assert.Contains(number, (await owner.GetAsync("/staff/invoices?q=" + anaCode)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain(number, (await owner.GetAsync("/staff/invoices?q=nobody")).Html, StringComparison.Ordinal);
        string invoicePage = WebUtility.HtmlDecode((await owner.GetAsync("/staff/invoices/" + invoiceId)).Html);
        Assert.Contains("Packing tape (TAPE)", invoicePage, StringComparison.Ordinal);
        Assert.Contains("Sales tax · Supplies 7 %", invoicePage, StringComparison.Ordinal);
        (HttpResponseMessage pdf, string pdfText) = await owner.GetAsync("/staff/invoices/" + invoiceId + "/pdf");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", pdfText, StringComparison.Ordinal);
        Assert.Matches("name=\"currencyCode\" disabled", (await owner.GetAsync("/staff/settings")).Html);

        // A credit note for one tape (4.50, its tax 0.32), asked by the owner: one request at a time, and never decided by the one who asked.
        string tapeLine = Regex.Matches(invoicePage, "name=\"amount_(?<id>[0-9a-f]{32})\"").Last().Groups["id"].Value;
        Assert.Equal("/staff/invoices/" + invoiceId + "?done=requested", (await owner.PostAsync("/staff/invoices/" + invoiceId + "/credits", invoicePage, ("key", Key()),
            ("amount_" + tapeLine, "4.50"), ("reason", "Damaged roll"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("already waiting for a decision", WebUtility.HtmlDecode((await owner.PostAsync("/staff/invoices/" + invoiceId + "/credits", invoicePage, ("key", Key()),
            ("amount_" + tapeLine, "1"), ("reason", "Again"))).Html), StringComparison.Ordinal);
        string waiting = WebUtility.HtmlDecode((await owner.GetAsync("/staff/invoices/" + invoiceId)).Html);
        Assert.Contains("Another person with the right to approve credits decides it.", waiting, StringComparison.Ordinal);
        string decide = Regex.Match(waiting, "/staff/invoices/credits/[0-9a-f-]{36}/decide").Value;
        Assert.Empty(decide);
        (HttpResponseMessage _, string requests) = await owner.GetAsync("/staff/invoices/credits");
        decide = Regex.Match(requests, "/staff/invoices/credits/[0-9a-f-]{36}/decide").Value;
        Assert.Empty(decide);

        // Finance, who may approve credits, approves it: numbered, and the invoice owes 4.82 less.
        using CookieClient finance = await PersonAsync(owner, "invoiceco", "Finance", "fin", "ops.invoices.view", "ops.credits.approve");
        (HttpResponseMessage _, string toApprove) = await finance.GetAsync("/staff/invoices/credits");
        Assert.Contains(number, toApprove, StringComparison.Ordinal);
        decide = Regex.Match(toApprove, "/staff/invoices/credits/[0-9a-f-]{36}/decide").Value;
        Assert.Equal("/staff/invoices/credits?done=approved", (await finance.PostAsync(decide, toApprove, ("approve", "true"), ("back", "credits"))).Response.Headers.Location?.OriginalString);
        Guid creditId;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsCreditNote credit = await context.CreditNotes.SingleAsync();
            Assert.Matches("^CN\\d{8}$", credit.Number);
            Assert.Equal(4.82m, (await context.Invoices.SingleAsync(item => item.Id == invoiceId)).CreditedTotal);
            creditId = credit.Id;
        }

        Assert.Equal("application/pdf", (await owner.GetAsync("/staff/invoices/" + invoiceId + "/pdf?credit=" + creditId)).Response.Content.Headers.ContentType?.MediaType);

        // An order of one tape has its own invoice; it is cancelled only by the approved void of that invoice, and its stock comes back.
        (HttpResponseMessage started, string _) = await owner.PostAsync(customerPath + "/orders", customerPage);
        string orderPath = started.Headers.Location?.OriginalString ?? string.Empty;
        (HttpResponseMessage _, string order) = await owner.GetAsync(orderPath);
        await owner.PostAsync(orderPath + "/supplies", order, ("key", Key()), ("scan", "0123"));
        Assert.Equal(orderPath + "?done=confirmed", (await owner.PostAsync(orderPath + "/confirm", order)).Response.Headers.Location?.OriginalString);
        Assert.Matches("Invoice INV\\d{8}", WebUtility.HtmlDecode((await owner.GetAsync(orderPath)).Html));
        Assert.Contains("ask for the void of the invoice", WebUtility.HtmlDecode((await owner.PostAsync(orderPath + "/cancel", order)).Html), StringComparison.Ordinal);
        Guid orderInvoice;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = await context.Invoices.SingleAsync(item => item.Kind == Vixly.Operations.Domain.Billing.OperationsInvoiceKind.SupplyOrder);
            Assert.Equal((4.82m, "Packing tape"), (invoice.Total, invoice.Lines.Single().Label));
            Assert.Equal(2, (await context.SupplyStocks.SingleAsync(stock => stock.SupplyId == tape)).OnHand);
            orderInvoice = invoice.Id;
        }

        string orderInvoicePage = (await owner.GetAsync("/staff/invoices/" + orderInvoice)).Html;
        Assert.Equal("/staff/invoices/" + orderInvoice + "?done=requested", (await owner.PostAsync("/staff/invoices/" + orderInvoice + "/credits", orderInvoicePage, ("key", Key()),
            ("void", "true"), ("reason", "Changed their mind"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string financeView) = await finance.GetAsync("/staff/invoices/" + orderInvoice);
        decide = Regex.Match(financeView, "/staff/invoices/credits/[0-9a-f-]{36}/decide").Value;
        Assert.Equal("/staff/invoices/" + orderInvoice + "?done=approved", (await finance.PostAsync(decide, financeView, ("approve", "true"), ("back", "invoice"))).Response.Headers.Location?.OriginalString);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Assert.Equal(Vixly.Operations.Domain.Billing.OperationsInvoiceStatus.Voided, (await context.Invoices.SingleAsync(item => item.Id == orderInvoice)).Status);
            Assert.Equal(Vixly.Operations.Domain.Supplies.OperationsSupplySaleStatus.Cancelled, (await context.SupplySales.SingleAsync(sale => sale.ShipmentId == null)).Status);
            Assert.Equal(3, (await context.SupplyStocks.SingleAsync(stock => stock.SupplyId == tape)).OnHand);
        }

        // A clerk at the counter prints the invoices of the counter but has no invoices page.
        using CookieClient clerk = await ClerkAsync(owner, "invoiceco");
        Assert.Contains("You do not have the right to see invoices.", WebUtility.HtmlDecode((await clerk.GetAsync("/staff/invoices")).Html), StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await clerk.GetAsync("/staff/invoices/" + invoiceId + "/pdf")).Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await finance.GetAsync("/staff/invoices/" + Guid.NewGuid() + "/pdf")).Response.StatusCode);
    }

    // Slice 5F step 2b: credit accounts: the credit department opens one; the cashier bills it and the shipment is released; above the limit the charge waits for another
    // person; paying at the cashier takes it off the account; the statement; a hold needs a reason and a close needs nothing owed.
    [SqlServerFact]
    public async Task Credit_TheCashierBillsTheAccount_AboveTheLimitTheCreditDepartmentDecides()
    {
        Guid tenant = NewTenant("creditco", "Credit Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("creditco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana.credit@example.com");
        Guid destination;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await context.TenantSettings.FirstOrDefaultAsync()
                ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(tenant, now);
            settings.RequireSecondFactor(true, now);
            settings.ChangeWork(Vixly.Operations.Domain.Settings.OperationsWeightUnit.Kilogram, Vixly.Operations.Domain.Settings.OperationsDimensionUnit.Centimetre, false, true,
                Vixly.Operations.Domain.Settings.OperationsDeliveryModes.Pickup, now);

            if (context.Entry(settings).State == EntityState.Detached)
            {
                context.Add(settings);
            }

            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        // Settings → Credit: 15 days; above the limit, ask the credit department.
        (HttpResponseMessage _, string creditSettings) = await owner.GetAsync("/staff/settings/credit");
        Assert.Equal("/staff/settings/credit?done=1", (await owner.PostAsync("/staff/settings/credit", creditSettings, ("terms", "15"), ("overLimit", "AskCreditDepartment"),
            ("statements", "true"))).Response.Headers.Location?.OriginalString);

        // Two final shipments with their invoices.
        string customerPath = "/staff/counter/customers/" + ana;
        async Task<Guid> ShipmentAsync()
        {
            (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
            (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
                ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
                ("recipientPhone", "+50937000000"));
            string path = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
            (HttpResponseMessage _, string before) = await owner.GetAsync(path);
            await owner.PostAsync(path + "/parcels", before, ("key", FormKey(before, path + "/parcels")), ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"),
                ("contents", "Clothes"), ("value", "40"));
            return Guid.Parse(path[^36..]);
        }

        Guid first = await ShipmentAsync();
        Guid second = await ShipmentAsync();
        Dictionary<Guid, Vixly.Operations.Domain.Billing.OperationsInvoice> invoices;

        async Task<Dictionary<Guid, Vixly.Operations.Domain.Billing.OperationsInvoice>> InvoicesAsync()
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Invoices.AsNoTracking().ToDictionaryAsync(invoice => invoice.ShipmentId!.Value);
        }

        invoices = await InvoicesAsync();
        string Money(decimal amount) => amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        // The credit department opens Ana's account with exactly the first invoice as its limit; the same form twice is one account.
        (HttpResponseMessage _, string accounts) = await owner.GetAsync("/staff/credit");
        (string, string)[] openForm = { ("code", anaCode), ("limit", Money(invoices[first].Total)), ("terms", "") };
        Assert.Equal("/staff/credit/customers/" + ana + "?done=opened", (await owner.PostAsync("/staff/credit/accounts", accounts, openForm)).Response.Headers.Location?.OriginalString);
        Assert.Equal("/staff/credit/customers/" + ana + "?done=opened", (await owner.PostAsync("/staff/credit/accounts", accounts, openForm)).Response.Headers.Location?.OriginalString);

        // The cashier bills the first invoice: on account, due in 15 days, the shipment released.
        string firstPath = "/staff/cashier/invoices/" + invoices[first].Id;
        (HttpResponseMessage _, string firstPage) = await owner.GetAsync(firstPath);
        Assert.Contains(firstPath + "/bill-account", firstPage, StringComparison.Ordinal);
        Assert.Equal(firstPath + "?done=billed", (await owner.PostAsync(firstPath + "/bill-account", firstPage)).Response.Headers.Location?.OriginalString);
        Vixly.Operations.Domain.Billing.OperationsInvoice billed = (await InvoicesAsync())[first];
        Assert.Equal((Vixly.Operations.Domain.Billing.OperationsPaymentState.OnAccount, true), (billed.PaymentState, billed.Released));
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15), billed.DueOn);
        Assert.Contains("On account", WebUtility.HtmlDecode((await owner.GetAsync("/staff/shipments")).Html), StringComparison.Ordinal);

        // The second goes above the limit: it waits; the owner asked, so a finance person decides.
        string secondPath = "/staff/cashier/invoices/" + invoices[second].Id;
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync(secondPath);
        Assert.Equal(secondPath + "?done=awaiting_credit", (await owner.PostAsync(secondPath + "/bill-account", secondPage)).Response.Headers.Location?.OriginalString);
        Assert.False((await InvoicesAsync())[second].Released);
        (HttpResponseMessage _, string charges) = await owner.GetAsync("/staff/credit/charges");
        Assert.Contains("You asked for this charge: another person decides it.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/credit/charges/" + invoices[second].Id, charges,
            ("approve", "true"))).Html), StringComparison.Ordinal);
        using CookieClient credit = await PersonWithSecondFactorAsync(owner, "creditco", "Credit", "credit", "ops.customers.credit");
        (HttpResponseMessage _, string toDecide) = await credit.GetAsync("/staff/credit/charges");
        Assert.Contains("Give a reason", WebUtility.HtmlDecode((await credit.PostAsync("/staff/credit/charges/" + invoices[second].Id, toDecide, ("approve", "false"))).Html),
            StringComparison.Ordinal);
        Assert.Equal("/staff/credit/charges?done=1", (await credit.PostAsync("/staff/credit/charges/" + invoices[second].Id, toDecide, ("approve", "true"))).Response.Headers.Location?.OriginalString);
        Assert.True((await InvoicesAsync())[second].Released);

        // The account: both owed, nothing available; the statement.
        string account = WebUtility.HtmlDecode((await owner.GetAsync("/staff/credit/customers/" + ana)).Html);
        Assert.Contains(invoices[first].Number, account, StringComparison.Ordinal);
        Assert.Contains(invoices[second].Number, account, StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await owner.GetAsync("/staff/credit/customers/" + ana + "/statement")).Response.Content.Headers.ContentType?.MediaType);

        // Paid at the cashier: off the account.
        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", warehouse.ToString()), ("float", "100"));
        (HttpResponseMessage _, string payFirst) = await owner.GetAsync(firstPath);
        await owner.PostAsync(firstPath + "/pay", payFirst, ("key", Key()), ("method_1", "Cash"), ("amount_1", Money(invoices[first].Total)));
        Assert.Equal(Vixly.Operations.Domain.Billing.OperationsPaymentState.Paid, (await InvoicesAsync())[first].PaymentState);

        // A hold needs a reason; a close needs nothing owed.
        string accountPath = "/staff/credit/customers/" + ana;
        (HttpResponseMessage _, string change) = await owner.GetAsync(accountPath);
        Assert.Contains("Give a reason", WebUtility.HtmlDecode((await owner.PostAsync(accountPath, change, ("limit", "50.00"), ("terms", ""), ("status", "OnHold"), ("reason", ""))).Html),
            StringComparison.Ordinal);
        Assert.Contains("can be closed only once nothing is owed", WebUtility.HtmlDecode((await owner.PostAsync(accountPath, change, ("limit", "50.00"), ("terms", ""), ("status", "Closed"),
            ("reason", "Moving away"))).Html), StringComparison.Ordinal);
        Assert.Equal(accountPath + "?done=changed", (await owner.PostAsync(accountPath, change, ("limit", "50.00"), ("terms", "20"), ("status", "OnHold"), ("reason", "Overdue")))
            .Response.Headers.Location?.OriginalString);
        Assert.Contains("on hold or closed", WebUtility.HtmlDecode((await owner.GetAsync(secondPath)).Html), StringComparison.Ordinal);

        // The monthly notice: none the month it was opened, then once next month for an account with something owed.
        for (int round = 0; round < 2; round++)
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Application.Billing.OperationsStatementNotices notices = scope.ServiceProvider.GetRequiredService<Vixly.Operations.Application.Billing.OperationsStatementNotices>();
            Assert.Equal(0, await notices.SendAsync(tenant, "creditco", DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Equal(round == 0 ? 1 : 0, await notices.SendAsync(tenant, "creditco", DateTimeOffset.UtcNow.AddMonths(1).AddDays(-DateTimeOffset.UtcNow.Day + 2), CancellationToken.None));
        }
    }

    // Slice 6A: bundles: the desk's ready groups, a bundle opened twice is one, the fill station refuses what does not belong (unpaid, unknown), warns past the target,
    // shows the shipment, takes out; closed with its weight and voided with a reason; a consolidated shipment repacked into a box that goes in whole.
    [SqlServerFact]
    public async Task Bundles_FillByScan_RefuseWhatDoesNotBelong_AndRepackConsolidatedShipments()
    {
        Guid tenant = NewTenant("bundleco", "Bundle Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("bundleco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.bundle@example.com");
        Guid destination;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup air = Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup.Create(tenant, "air_standard", now);
            air.Change(true, null, 1, 5, 5000m, true, now);
            context.AddRange(haiti, pickup, air);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        // Settings → Bundles: the company's types; consolidated shipments are repacked.
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/bundles");
        Assert.Equal("/staff/settings/bundles?done=1", (await owner.PostAsync("/staff/settings/bundles", settingsPage, ("types", "Sack\nPallet"), ("rule", "Warn"), ("way", "Repack")))
            .Response.Headers.Location?.OriginalString);

        // Three final shipments: one to pay, one left unpaid, one consolidated with two parcels.
        string customerPath = "/staff/counter/customers/" + ana;
        async Task<Guid> ShipmentAsync(int parcels, bool consolidate)
        {
            (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
            List<(string, string)> fields = new() { ("key", FormKey(customerPage, customerPath + "/shipments")), ("origin", warehouse.ToString()), ("destination", destination.ToString()),
                ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", parcels.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("recipient", "Jean Baptiste"),
                ("recipientPhone", "+50937000000") };
            if (consolidate)
            {
                fields.Add(("consolidate", "true"));
            }

            (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, fields.ToArray());
            string path = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;

            for (int piece = 0; piece < parcels; piece++)
            {
                (HttpResponseMessage _, string before) = await owner.GetAsync(path);
                await owner.PostAsync(path + "/parcels", before, ("key", FormKey(before, path + "/parcels")), ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"),
                    ("contents", "Clothes"), ("value", "40"));
            }

            return Guid.Parse(path[^36..]);
        }

        Guid paid = await ShipmentAsync(1, false);
        Guid unpaid = await ShipmentAsync(1, false);
        Guid consolidated = await ShipmentAsync(2, true);
        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", warehouse.ToString()), ("float", "100"));
        Dictionary<Guid, string> numbers = new();

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();

            foreach (Vixly.Operations.Domain.Billing.OperationsInvoice invoice in await context.Invoices.Where(item => item.ShipmentId == paid || item.ShipmentId == consolidated).ToListAsync())
            {
                (HttpResponseMessage _, string payPage) = await owner.GetAsync("/staff/cashier/invoices/" + invoice.Id);
                await owner.PostAsync("/staff/cashier/invoices/" + invoice.Id + "/pay", payPage, ("key", Key()), ("method_1", "Cash"),
                    ("amount_1", invoice.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            }

            foreach (OperationsParcel parcel in await context.Parcels.Where(item => item.ShipmentId == paid || item.ShipmentId == unpaid || item.ShipmentId == consolidated).ToListAsync())
            {
                numbers[parcel.Id] = parcel.Number!;
            }
        }

        async Task<OperationsParcel> ParcelAsync(Guid shipment)
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.AsNoTracking().FirstAsync(item => item.ShipmentId == shipment);
        }

        // The desk: one parcel ready; the unpaid one and the consolidated ones (to repack first) wait.
        string desk = WebUtility.HtmlDecode((await owner.GetAsync("/staff/bundles")).Html);
        Assert.Contains("PAP1 · Capois", desk, StringComparison.Ordinal);
        Assert.Contains("shipments: 1", desk, StringComparison.Ordinal);

        // A bundle with a 2 kg target, opened twice with the same form: one bundle.
        (HttpResponseMessage _, string deskPage) = await owner.GetAsync("/staff/bundles");
        string bundleKey = Key();
        (string, string)[] bundleForm = { ("key", bundleKey), ("warehouse", warehouse.ToString()), ("destination", "w:" + destination.ToString("N")), ("service", "air_standard"), ("type", "Sack"),
            ("basis", "Weight"), ("target", "2") };
        string bundlePath = Regex.Match((await owner.PostAsync("/staff/bundles", deskPage, bundleForm)).Response.Headers.Location?.OriginalString ?? string.Empty,
            "^/staff/bundles/[0-9a-f-]{36}").Value;
        Assert.NotEmpty(bundlePath);
        Assert.StartsWith(bundlePath + "?", (await owner.PostAsync("/staff/bundles", deskPage, bundleForm)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Contains("Choose one of the company’s bundle types.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/bundles", deskPage, ("key", Key()),
            ("warehouse", warehouse.ToString()), ("destination", "w:" + destination.ToString("N")), ("service", "air_standard"), ("type", "Crate"), ("basis", "Free"))).Html), StringComparison.Ordinal);
        string bundleNumber = Regex.Match((await owner.GetAsync(bundlePath)).Html, "Bundle (?<number>BD[0-9]+)").Groups["number"].Value;
        Assert.Matches("^BD\\d{8}$", bundleNumber);
        Assert.Equal("application/pdf", (await owner.GetAsync(bundlePath + "/label")).Response.Content.Headers.ContentType?.MediaType);

        // The fill station: the bundle's barcode first, then parcels.
        (HttpResponseMessage _, string station) = await owner.GetAsync("/staff/bundles/fill");
        Assert.Equal(bundlePath + "/fill", (await owner.PostAsync("/staff/bundles/fill", station, ("code", bundleNumber.ToLowerInvariant()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string fill) = await owner.GetAsync(bundlePath + "/fill");
        Task<(HttpResponseMessage Response, string Html)> Scan(string code) => owner.PostAsync(bundlePath + "/scan", fill, ("key", Key()), ("code", code));

        OperationsParcel waiting = await ParcelAsync(unpaid);
        Assert.Contains("Refused: its shipment is not paid yet.", WebUtility.HtmlDecode((await Scan(waiting.Number!)).Html), StringComparison.Ordinal);
        Assert.Contains("This barcode is not a parcel, a bundle or a box here.", WebUtility.HtmlDecode((await Scan("PK00000000")).Html), StringComparison.Ordinal);
        OperationsParcel loose = (await ParcelAsync(consolidated));
        Assert.Contains("is repacked first", WebUtility.HtmlDecode((await Scan(loose.Number!)).Html).Replace("’", "'", StringComparison.Ordinal), StringComparison.Ordinal);

        OperationsParcel ready = await ParcelAsync(paid);
        string added = (await Scan(ready.Number!)).Response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Equal(bundlePath + "/fill?note=past.added&parcel=" + ready.Id, added);
        string afterScan = WebUtility.HtmlDecode((await owner.GetAsync(added)).Html);
        Assert.Contains("The bundle is past its target.", afterScan, StringComparison.Ordinal);
        Assert.Contains("1 of 1 in bundles, 1 here", afterScan, StringComparison.Ordinal);
        Assert.Contains("note=repeat", (await Scan(ready.Number!)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal((OperationsMilestone.Consolidated, true), ((await ParcelAsync(paid)).Milestone, (await ParcelAsync(paid)).BundleId is not null));

        // Taken out (a wrong scan): back to final; scanned again.
        (HttpResponseMessage _, string filled) = await owner.GetAsync(bundlePath + "/fill");
        Assert.Equal(bundlePath + "/fill?note=out", (await owner.PostAsync(bundlePath + "/take-out", filled, ("parcel", ready.Id.ToString()))).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.Booked, (await ParcelAsync(paid)).Milestone);
        await Scan(ready.Number!);

        // The consolidated shipment is repacked: its two parcels into one box, closed, then the box into the bundle.
        string boxes = WebUtility.HtmlDecode((await owner.GetAsync("/staff/bundles/boxes")).Html);
        Assert.Contains("Repacked into one box", boxes, StringComparison.Ordinal);
        (HttpResponseMessage started, string _) = await owner.PostAsync("/staff/bundles/boxes", (await owner.GetAsync("/staff/bundles/boxes")).Html, ("shipment", consolidated.ToString()), ("key", Key()));
        string boxPath = started.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Matches("^/staff/bundles/boxes/[0-9a-f-]{36}$", boxPath);
        (HttpResponseMessage _, string boxPage) = await owner.GetAsync(boxPath);
        string boxNumber = Regex.Match(WebUtility.HtmlDecode(boxPage), "Repack box (?<number>CB[0-9]+)").Groups["number"].Value;
        Assert.Contains("Every parcel of the shipment", WebUtility.HtmlDecode((await owner.PostAsync(boxPath + "/scan", boxPage, ("key", Key()), ("code", loose.Number!))).Html) +
            WebUtility.HtmlDecode((await owner.PostAsync(boxPath + "/close", boxPage, ("weight", "6"), ("length", "40"), ("width", "30"), ("height", "20"))).Html), StringComparison.Ordinal);
        Assert.Contains("Close the box with its weight and size first.", WebUtility.HtmlDecode((await Scan(boxNumber)).Html), StringComparison.Ordinal);

        foreach (string number in numbers.Where(pair => pair.Key != loose.Id).Select(pair => pair.Value))
        {
            await owner.PostAsync(boxPath + "/scan", boxPage, ("key", Key()), ("code", number));
        }

        Assert.Contains("note=closed", (await owner.PostAsync(boxPath + "/close", boxPage, ("weight", "6"), ("length", "40"), ("width", "30"), ("height", "20"))).Response.Headers.Location
            ?.OriginalString, StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await owner.GetAsync(boxPath + "/label")).Response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("note=past.added", (await Scan(boxNumber)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        string bundlePage = WebUtility.HtmlDecode((await owner.GetAsync(bundlePath)).Html);
        Assert.Contains(boxNumber, bundlePage, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Assert.Equal(Vixly.Operations.Domain.Bundles.OperationsConsolidationWay.Repack,
                (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Shipments.AsNoTracking().SingleAsync(item => item.Id == consolidated)).ConsolidationWay);
        }

        // Closed with its weight and size; voided with a reason, its parcels and box are ready again.
        (HttpResponseMessage _, string closePage) = await owner.GetAsync(bundlePath);
        Assert.Equal(bundlePath + "?done=closed", (await owner.PostAsync(bundlePath + "/close", closePage, ("weight", "9.5"), ("length", "60"), ("width", "40"), ("height", "40"))).Response
            .Headers.Location?.OriginalString);
        Assert.Contains("This bundle is not open.", WebUtility.HtmlDecode((await Scan(ready.Number!)).Html), StringComparison.Ordinal);
        (HttpResponseMessage _, string voidPage) = await owner.GetAsync(bundlePath);
        Assert.Contains("Give a short reason.", WebUtility.HtmlDecode((await owner.PostAsync(bundlePath + "/void", voidPage, ("reason", " "))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/bundles?done=voided", (await owner.PostAsync(bundlePath + "/void", voidPage, ("reason", "Wrong flight"))).Response.Headers.Location?.OriginalString);
        Assert.Equal((OperationsMilestone.Booked, (Guid?)null), ((await ParcelAsync(paid)).Milestone, (await ParcelAsync(paid)).BundleId));

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Bundles.OperationsRepackBox box = await context.RepackBoxes.AsNoTracking().SingleAsync();
            Assert.Equal((Vixly.Operations.Domain.Bundles.OperationsRepackBoxStatus.Closed, (Guid?)null), (box.Status, box.BundleId));
            Assert.Equal(new[] { "ops.tracking.bundle_closed", "ops.tracking.bundle_created", "ops.tracking.bundle_voided" },
                (await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectKind == OperationsTrackingSubject.Bundle).Select(item => item.Code).ToListAsync()).Order(StringComparer.Ordinal));
        }

        // A worker who only fills bundles cannot open one.
        using CookieClient worker = await PersonAsync(owner, "bundleco", "Bundler", "bundler", "ops.bundles.fill");
        (HttpResponseMessage _, string workerDesk) = await worker.GetAsync("/staff/bundles");
        Assert.DoesNotContain("action=\"/staff/bundles\"", workerDesk, StringComparison.Ordinal);
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.PostAsync("/staff/bundles", workerDesk, bundleForm.Select(field => field.Item1 == "key" ? ("key", Key()) : field).ToArray())).Html).ToLowerInvariant(), StringComparison.Ordinal);
    }

    // Slice 6B step 1: carriers and rates; a dispatch takes a closed bundle, is closed with a leg, departs and arrives; the parcel follows and the customer sees it; manifests print.
    [SqlServerFact]
    public async Task Dispatch_ManifestsBundles_DepartsAndArrives_AndTheParcelFollows()
    {
        Guid tenant = NewTenant("dispatchco", "Dispatch Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("dispatchco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.dispatch@example.com");
        Guid destination;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup air = Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup.Create(tenant, "air_standard", now);
            air.Change(true, null, 1, 5, 5000m, true, now);
            context.AddRange(haiti, pickup, air);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        // Carriers: an airline with its prefix; a bad prefix and a second carrier of the same name are refused; a rate per kg from today.
        (HttpResponseMessage _, string carriersPage) = await owner.GetAsync("/staff/carriers");
        Assert.Contains("An airline prefix is three digits", WebUtility.HtmlDecode((await owner.PostAsync("/staff/carriers", carriersPage, ("name", "Ocean Line"), ("kind", "Ocean"),
            ("prefix", "123"))).Html), StringComparison.Ordinal);
        string carrierPath = (await owner.PostAsync("/staff/carriers", carriersPage, ("name", "Sky Cargo"), ("kind", "Air"), ("account", "ACC-77"), ("prefix", "123"))).Response.Headers.Location
            ?.OriginalString ?? string.Empty;
        Assert.Matches("^/staff/carriers/[0-9a-f-]{36}\\?done=saved$", carrierPath);
        Assert.Contains("Another carrier already has that name.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/carriers", carriersPage, ("name", "sky cargo"), ("kind", "Air"))).Html),
            StringComparison.Ordinal);
        string carrier = carrierPath[..carrierPath.IndexOf('?', StringComparison.Ordinal)];
        (HttpResponseMessage _, string carrierPage) = await owner.GetAsync(carrier);
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("Check the rate", WebUtility.HtmlDecode((await owner.PostAsync(carrier + "/rates", carrierPage, ("destination", "w:" + destination.ToString("N")), ("perBundle", "40"),
            ("starts", today))).Html), StringComparison.Ordinal);
        Assert.Equal(carrier + "?done=rate", (await owner.PostAsync(carrier + "/rates", carrierPage, ("destination", "w:" + destination.ToString("N")), ("perWeight", "2.5"), ("minimum", "50"),
            ("starts", today))).Response.Headers.Location?.OriginalString);
        Assert.Contains("50.00 USD", WebUtility.HtmlDecode((await owner.GetAsync(carrier)).Html), StringComparison.Ordinal);

        // One paid shipment, bundled and closed.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        (HttpResponseMessage _, string shipmentPage) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/parcels", shipmentPage, ("key", FormKey(shipmentPage, shipmentPath + "/parcels")), ("weight", "4"), ("length", "30"), ("width", "20"),
            ("height", "10"), ("contents", "Shoes"), ("value", "60"));
        Guid shipment = Guid.Parse(shipmentPath[^36..]);
        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", warehouse.ToString()), ("float", "100"));
        OperationsParcel parcel;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = await context.Invoices.AsNoTracking().SingleAsync(item => item.ShipmentId == shipment);
            (HttpResponseMessage _, string payPage) = await owner.GetAsync("/staff/cashier/invoices/" + invoice.Id);
            await owner.PostAsync("/staff/cashier/invoices/" + invoice.Id + "/pay", payPage, ("key", Key()), ("method_1", "Cash"),
                ("amount_1", invoice.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            parcel = await context.Parcels.AsNoTracking().SingleAsync(item => item.ShipmentId == shipment);
        }

        async Task<OperationsParcel> ParcelAsync()
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.AsNoTracking().SingleAsync(item => item.Id == parcel.Id);
        }

        (HttpResponseMessage _, string deskPage) = await owner.GetAsync("/staff/bundles");
        string bundlePath = Regex.Match((await owner.PostAsync("/staff/bundles", deskPage, ("key", Key()), ("warehouse", warehouse.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"), ("basis", "Free"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/bundles/[0-9a-f-]{36}").Value;
        string bundleNumber = Regex.Match((await owner.GetAsync(bundlePath)).Html, "Bundle (?<number>BD[0-9]+)").Groups["number"].Value;
        (HttpResponseMessage _, string fill) = await owner.GetAsync(bundlePath + "/fill");
        await owner.PostAsync(bundlePath + "/scan", fill, ("key", Key()), ("code", parcel.Number!));
        (HttpResponseMessage _, string closePage) = await owner.GetAsync(bundlePath);
        await owner.PostAsync(bundlePath + "/close", closePage, ("weight", "5"), ("length", "40"), ("width", "30"), ("height", "20"));
        Assert.Equal("application/pdf", (await owner.GetAsync(bundlePath + "/manifest")).Response.Content.Headers.ContentType?.MediaType);

        // A dispatch planned twice with the same form: one dispatch; the bundle fits it.
        (HttpResponseMessage _, string dispatchesPage) = await owner.GetAsync("/staff/dispatches");
        Assert.Contains("1 closed bundles not on a dispatch", WebUtility.HtmlDecode(dispatchesPage), StringComparison.Ordinal);
        (string, string)[] dispatchForm = { ("key", Key()), ("warehouse", warehouse.ToString()), ("destination", "w:" + destination.ToString("N")), ("service", "air_standard"),
            ("cutoff", DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture)) };
        string dispatchPath = Regex.Match((await owner.PostAsync("/staff/dispatches", dispatchesPage, dispatchForm)).Response.Headers.Location?.OriginalString ?? string.Empty,
            "^/staff/dispatches/[0-9a-f-]{36}").Value;
        Assert.NotEmpty(dispatchPath);
        Assert.StartsWith(dispatchPath + "?", (await owner.PostAsync("/staff/dispatches", dispatchesPage, dispatchForm)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string dispatchPage) = await owner.GetAsync(dispatchPath);
        Assert.Matches("Dispatch DSP\\d{8}", WebUtility.HtmlDecode(dispatchPage));
        Assert.Contains(bundleNumber, dispatchPage, StringComparison.Ordinal);

        // Closing needs a leg; the bundle is scanned in (twice: once); then a leg with the carrier; closed: the parcel is manifested and the bundle can no longer be voided.
        Assert.Equal(dispatchPath + "?done=bundle", (await owner.PostAsync(dispatchPath + "/bundles", dispatchPage, ("number", bundleNumber.ToLowerInvariant()))).Response.Headers.Location?.OriginalString);
        Assert.Equal(dispatchPath + "?done=bundle", (await owner.PostAsync(dispatchPath + "/bundles", dispatchPage, ("number", bundleNumber))).Response.Headers.Location?.OriginalString);
        Assert.Contains("A dispatch needs at least one bundle and one leg to close.", WebUtility.HtmlDecode((await owner.PostAsync(dispatchPath + "/close", dispatchPage)).Html),
            StringComparison.Ordinal);
        Assert.Equal(dispatchPath + "?done=leg", (await owner.PostAsync(dispatchPath + "/legs", dispatchPage, ("carrier", carrier[^36..]), ("kind", "Ground"), ("from", "MIA"), ("to", "PAP"),
            ("reference", "123-45678901"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(dispatchPath + "?done=closed", (await owner.PostAsync(dispatchPath + "/close", dispatchPage)).Response.Headers.Location?.OriginalString);
        Assert.Equal(dispatchPath + "?done=closed", (await owner.PostAsync(dispatchPath + "/close", dispatchPage)).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.Manifested, (await ParcelAsync()).Milestone);
        (HttpResponseMessage _, string voidPage) = await owner.GetAsync(bundlePath);
        Assert.Contains("can no longer be voided", WebUtility.HtmlDecode((await owner.PostAsync(bundlePath + "/void", voidPage, ("reason", "Wrong flight"))).Html), StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await owner.GetAsync(dispatchPath + "/manifest")).Response.Content.Headers.ContentType?.MediaType);

        // Step 2: the shipment has its house number under the dispatch; its house air waybill and the letter of instruction print; the instructions are kept.
        string closedPage = WebUtility.HtmlDecode((await owner.GetAsync(dispatchPath)).Html);
        string dispatchNumber = Regex.Match(closedPage, "Dispatch (?<number>DSP[0-9]+)").Groups["number"].Value;
        Assert.Contains(dispatchNumber + "-001", closedPage, StringComparison.Ordinal);
        Assert.Contains("Worked out when the dispatch leaves", closedPage, StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await owner.GetAsync(dispatchPath + "/hawb?shipment=" + shipment)).Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/pdf", (await owner.GetAsync(dispatchPath + "/hawb")).Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(dispatchPath + "/hawb?shipment=" + Guid.NewGuid())).Response.StatusCode);
        Assert.Equal(dispatchPath + "?done=instructions", (await owner.PostAsync(dispatchPath + "/instructions", dispatchPage, ("collect", "false"), ("exportReference", "X20261021000001"),
            ("documents", "CommercialInvoice"), ("documents", "PackingList"), ("handling", "Keep dry"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("application/pdf", (await owner.GetAsync(dispatchPath + "/sli")).Response.Content.Headers.ContentType?.MediaType);

        // The flight is cancelled after closing: the parcel goes back to consolidated, the bundle is free and its house numbers go.
        string firstPath = dispatchPath;
        Assert.Equal(firstPath + "?done=cancelled", (await owner.PostAsync(firstPath + "/cancel", dispatchPage, ("reason", "Flight cancelled"))).Response.Headers.Location?.OriginalString);
        Assert.Equal((OperationsMilestone.Consolidated, false), ((await ParcelAsync()).Milestone, (await owner.GetAsync(firstPath + "/hawb")).Response.IsSuccessStatusCode));

        // Planned again with two legs: the airline to SDQ, then a truck to PAP (the same leg form posted twice adds one leg).
        (HttpResponseMessage _, string desk2) = await owner.GetAsync("/staff/dispatches");
        dispatchPath = Regex.Match((await owner.PostAsync("/staff/dispatches", desk2, ("key", Key()), ("warehouse", warehouse.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/dispatches/[0-9a-f-]{36}").Value;
        Assert.NotEqual(firstPath, dispatchPath);
        (HttpResponseMessage _, string planned) = await owner.GetAsync(dispatchPath);
        dispatchPage = planned;
        Assert.Contains(bundleNumber, dispatchPage, StringComparison.Ordinal);
        await owner.PostAsync(dispatchPath + "/bundles", dispatchPage, ("number", bundleNumber));
        string legKey = Key();
        (string, string)[] firstLeg = { ("key", legKey), ("carrier", carrier[^36..]), ("kind", "Air"), ("from", "MIA"), ("to", "SDQ"), ("reference", "45678901") };
        await owner.PostAsync(dispatchPath + "/legs", dispatchPage, firstLeg);
        await owner.PostAsync(dispatchPath + "/legs", dispatchPage, firstLeg);
        await owner.PostAsync(dispatchPath + "/legs", dispatchPage, ("key", Key()), ("kind", "Ground"), ("from", "SDQ"), ("to", "PAP"), ("driver", "Paul"));
        Assert.Equal(dispatchPath + "?done=closed", (await owner.PostAsync(dispatchPath + "/close", dispatchPage)).Response.Headers.Location?.OriginalString);
        dispatchNumber = Regex.Match(WebUtility.HtmlDecode((await owner.GetAsync(dispatchPath)).Html), "Dispatch (?<number>DSP[0-9]+)").Groups["number"].Value;
        await owner.PostAsync(dispatchPath + "/instructions", dispatchPage, ("collect", "false"), ("exportReference", "X20261021000001"), ("documents", "CommercialInvoice"), ("documents", "PackingList"));

        // The legs leave and arrive in order (the first by air: its carrier's kind): the parcel departed the origin, went in transit at SDQ and reached the destination country, at the times typed.
        Guid leg;
        Guid truck;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Domain.Dispatch.OperationsDispatch dispatch = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Dispatches.AsNoTracking()
                .SingleAsync(item => item.Status == Vixly.Operations.Domain.Dispatch.OperationsDispatchStatus.Closed);
            Assert.Equal(2, dispatch.Legs.Count);
            leg = dispatch.Legs.Single(item => item.Sequence == 1).Id;
            truck = dispatch.Legs.Single(item => item.Sequence == 2).Id;
            Assert.Equal(Vixly.Operations.Domain.Dispatch.OperationsCarrierKind.Air, dispatch.Legs.Single(item => item.Id == leg).Kind);
            Assert.Equal("123-45678901", dispatch.MasterWaybill(await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Carriers.AsNoTracking().ToDictionaryAsync(item => item.Id)));
        }

        // A few minutes ago, never yesterday: the carrier's rate starts today (two hours ago failed in the first hours of a UTC day).
        string at = DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("Legs leave in order", WebUtility.HtmlDecode((await owner.PostAsync(dispatchPath + "/legs/" + leg + "/arrive", dispatchPage, ("at", at))).Html), StringComparison.Ordinal);
        Assert.Contains("Check the time", WebUtility.HtmlDecode((await owner.PostAsync(dispatchPath + "/legs/" + leg + "/depart", dispatchPage, ("at", DateTime.UtcNow.AddDays(2)
            .ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture)))).Html), StringComparison.Ordinal);
        Assert.Equal(dispatchPath + "?done=departed", (await owner.PostAsync(dispatchPath + "/legs/" + leg + "/depart", dispatchPage, ("at", at))).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.DepartedOrigin, (await ParcelAsync()).Milestone);

        // The cost at departure: 5 kg chargeable at 2.50 is below the 50.00 minimum; the shipment carries it all. Corrected from the carrier's invoice, with a note.
        string departedPage = WebUtility.HtmlDecode((await owner.GetAsync(dispatchPath)).Html);
        Assert.Contains("50.00 USD", departedPage, StringComparison.Ordinal);
        Assert.Contains("Give a reason.", WebUtility.HtmlDecode((await owner.PostAsync(dispatchPath + "/cost", dispatchPage, ("total", "75"), ("note", ""))).Html), StringComparison.Ordinal);
        Assert.Equal(dispatchPath + "?done=cost", (await owner.PostAsync(dispatchPath + "/cost", dispatchPage, ("total", "75"), ("note", "Fuel surcharge"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(dispatchPath + "?done=costed", (await owner.PostAsync(dispatchPath + "/recost", dispatchPage)).Response.Headers.Location?.OriginalString);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Dispatch.OperationsDispatch costed = await context.Dispatches.AsNoTracking().SingleAsync(item => item.Status == Vixly.Operations.Domain.Dispatch.OperationsDispatchStatus.Departed);
            Assert.Equal((50m, 75m, "Fuel surcharge"), (costed.CostComputed!.Value, costed.CostTotal!.Value, costed.CostNote));
            Vixly.Operations.Domain.Dispatch.OperationsDispatchShipment row = await context.DispatchShipments.AsNoTracking().SingleAsync();
            Assert.Equal((dispatchNumber + "-001", 1, 75m), (row.HouseNumber, row.Pieces, row.CostShare!.Value));
            Assert.Equal(("X20261021000001", Vixly.Operations.Domain.Dispatch.OperationsDispatchDocuments.CommercialInvoice | Vixly.Operations.Domain.Dispatch.OperationsDispatchDocuments.PackingList),
                (costed.ExportReference, costed.Documents));
        }
        Assert.Contains("A dispatch that has left cannot be cancelled.", WebUtility.HtmlDecode((await owner.PostAsync(dispatchPath + "/cancel", dispatchPage, ("reason", "Late"))).Html),
            StringComparison.Ordinal);
        Assert.Equal(dispatchPath + "?done=arrived", (await owner.PostAsync(dispatchPath + "/legs/" + leg + "/arrive", dispatchPage)).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.DepartedOrigin, (await ParcelAsync()).Milestone);
        Assert.Equal(dispatchPath + "?done=departed", (await owner.PostAsync(dispatchPath + "/legs/" + truck + "/depart", dispatchPage)).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.InTransit, (await ParcelAsync()).Milestone);
        Assert.Equal(dispatchPath + "?done=arrived", (await owner.PostAsync(dispatchPath + "/legs/" + truck + "/arrive", dispatchPage)).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.ArrivedDestinationCountry, (await ParcelAsync()).Milestone);
        Assert.Contains("Arrived", WebUtility.HtmlDecode((await owner.GetAsync(dispatchPath)).Html), StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            List<OperationsTrackingEvent> events = await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectId == parcel.Id).ToListAsync();
            OperationsTrackingEvent departed = events.Single(item => item.Code == "ops.tracking.departed_origin");
            Assert.Equal("air", departed.Arguments()["mode"]);
            Assert.True(Math.Abs((departed.OccurredUtc - DateTimeOffset.UtcNow.AddMinutes(-5)).TotalMinutes) < 2);
            Assert.Equal(2, events.Count(item => item.Code == "ops.tracking.manifested"));
            Assert.Single(events, item => item.Code == "ops.tracking.event_voided");
            Assert.Equal("SDQ", events.Single(item => item.Code == "ops.tracking.in_transit").Arguments()["place"]);
            Assert.Single(events, item => item.Code == "ops.tracking.arrived_destination_country");
            Guid second = Guid.Parse(dispatchPath[^36..]);
            Assert.Equal(new[] { "ops.tracking.dispatch_arrived", "ops.tracking.dispatch_arrived", "ops.tracking.dispatch_closed", "ops.tracking.dispatch_departed", "ops.tracking.dispatch_departed" },
                (await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectKind == OperationsTrackingSubject.Dispatch && item.SubjectId == second).Select(item => item.Code).ToListAsync())
                    .Order(StringComparer.Ordinal));
        }

        // A second dispatch, closed with nothing to close, is cancelled with a reason.
        (HttpResponseMessage _, string again) = await owner.GetAsync("/staff/dispatches");
        string secondPath = Regex.Match((await owner.PostAsync("/staff/dispatches", again, ("key", Key()), ("warehouse", warehouse.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/dispatches/[0-9a-f-]{36}").Value;
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync(secondPath);
        Assert.Contains("Give a reason.", WebUtility.HtmlDecode((await owner.PostAsync(secondPath + "/cancel", secondPage, ("reason", " "))).Html), StringComparison.Ordinal);
        Assert.Equal(secondPath + "?done=cancelled", (await owner.PostAsync(secondPath + "/cancel", secondPage, ("reason", "Flight full"))).Response.Headers.Location?.OriginalString);

        // Someone without the right sees neither page.
        using CookieClient worker = await PersonAsync(owner, "dispatchco", "Bundler", "bundler.dispatch", "ops.bundles.fill");
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.GetAsync("/staff/dispatches")).Html).ToLowerInvariant(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await worker.GetAsync(dispatchPath)).Response.StatusCode);
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.GetAsync("/staff/carriers")).Html).ToLowerInvariant(), StringComparison.Ordinal);
    }

    // Slice 6C: the events workspace finds a shipment by its number; notes stay internal; a delay on the shipment reaches its parcel and the customer (one email);
    // the company's own cause; a standard cause switched off; a void takes the copies with it; someone without the right sees nothing.
    [SqlServerFact]
    public async Task Events_RecordNotesDelaysAndExceptions_ReachTheParcels_AndAreVoidedWithTheirCopies()
    {
        Guid tenant = NewTenant("eventco", "Event Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("eventco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.events@example.com");
        Guid destination;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup air = Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup.Create(tenant, "air_standard", now);
            air.Change(true, null, 1, 5, 5000m, true, now);
            context.AddRange(haiti, pickup, air);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        (HttpResponseMessage _, string shipmentPage) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/parcels", shipmentPage, ("key", FormKey(shipmentPage, shipmentPath + "/parcels")), ("weight", "4"), ("length", "30"), ("width", "20"),
            ("height", "10"), ("contents", "Shoes"), ("value", "60"));
        Guid shipment = Guid.Parse(shipmentPath[^36..]);
        string number;
        OperationsParcel parcel;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            number = (await context.Shipments.AsNoTracking().SingleAsync(item => item.Id == shipment)).Number!;
            parcel = await context.Parcels.AsNoTracking().SingleAsync(item => item.ShipmentId == shipment);
        }

        // Found by its number (typed in lower case); an unknown number is not found.
        (HttpResponseMessage _, string search) = await owner.GetAsync("/staff/events");
        Assert.Contains("Nothing with that number was found here.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/events", search, ("query", "NOPE123"))).Html), StringComparison.Ordinal);
        string timeline = (await owner.PostAsync("/staff/events", search, ("query", number.ToLowerInvariant()))).Response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Equal("/staff/events/Shipment/" + shipment, timeline);
        (HttpResponseMessage _, string page) = await owner.GetAsync(timeline);

        // A note needs its text and stays internal; a delay needs a cause; the same form posted twice records one.
        Assert.Contains("Write the note.", WebUtility.HtmlDecode((await owner.PostAsync(timeline, page, ("key", Key()), ("eventKind", "Note"), ("note", " "))).Html), StringComparison.Ordinal);
        Assert.Equal(timeline + "?done=recorded", (await owner.PostAsync(timeline, page, ("key", Key()), ("eventKind", "Note"), ("note", "Customer called about the shoes"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Choose a cause the company offers.", WebUtility.HtmlDecode((await owner.PostAsync(timeline, page, ("key", Key()), ("eventKind", "Delay"), ("cause", "nonsense"))).Html), StringComparison.Ordinal);
        string delayKey = Key();
        (string, string)[] delay = { ("key", delayKey), ("eventKind", "Delay"), ("cause", "weather"), ("message", "Storm in Miami") };
        Assert.Equal(timeline + "?done=recorded", (await owner.PostAsync(timeline, page, delay)).Response.Headers.Location?.OriginalString);
        Assert.Equal(timeline + "?done=recorded", (await owner.PostAsync(timeline, page, delay)).Response.Headers.Location?.OriginalString);
        string history = WebUtility.HtmlDecode((await owner.GetAsync(timeline)).Html);
        Assert.Contains("Delayed: Bad weather — Storm in Miami", history, StringComparison.Ordinal);
        Assert.Contains("Customer called about the shoes", history, StringComparison.Ordinal);
        Assert.Contains("Delayed: Bad weather — Storm in Miami · via " + number, WebUtility.HtmlDecode((await owner.GetAsync("/staff/events/Parcel/" + parcel.Id)).Html), StringComparison.Ordinal);

        Guid delayId;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsTrackingEvent source = await context.TrackingEvents.AsNoTracking().SingleAsync(item => item.SubjectId == shipment && item.Code == "ops.tracking.delayed");
            delayId = source.Id;
            OperationsTrackingEvent copy = await context.TrackingEvents.AsNoTracking().SingleAsync(item => item.SubjectId == parcel.Id && item.Code == "ops.tracking.delayed");
            Assert.Equal((source.Id, OperationsTrackingVisibility.Customer), (copy.CausedByEventId!.Value, copy.Visibility));
            Assert.Equal(1, await context.OutboxMessages.AsNoTracking().CountAsync(item => item.Type == "ops.events.update.v1" && item.TenantId == tenant));
            Assert.Equal(OperationsTrackingVisibility.Internal, (await context.TrackingEvents.AsNoTracking().SingleAsync(item => item.Code == "ops.tracking.note")).Visibility);
        }

        // Settings → Events: the company's own cause (English required); a standard cause switched off is no longer offered; emails switched off.
        (HttpResponseMessage _, string settings) = await owner.GetAsync("/staff/settings/events");
        Assert.Contains("Check the text", WebUtility.HtmlDecode((await owner.PostAsync("/staff/settings/events/causes", settings, ("kind", "Exception"), ("en", "<b>x</b>"))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/settings/events?done=1", (await owner.PostAsync("/staff/settings/events/causes", settings, ("kind", "Exception"), ("en", "Wrong size declared"),
            ("fr", "Mauvaise taille déclarée"))).Response.Headers.Location?.OriginalString);
        List<(string, string)> keep = Vixly.Operations.Domain.Events.OperationsEventReasons.Delay.Concat(Vixly.Operations.Domain.Events.OperationsEventReasons.Exception)
            .Where(code => code != "weather").Select(code => ("enabled", code)).ToList();
        Assert.Equal("/staff/settings/events?done=1", (await owner.PostAsync("/staff/settings/events", settings, keep.ToArray())).Response.Headers.Location?.OriginalString);
        string own;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            own = (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().EventReasons.AsNoTracking().SingleAsync()).Code;
        }

        string parcelTimeline = "/staff/events/Parcel/" + parcel.Id;
        (HttpResponseMessage _, string parcelPage) = await owner.GetAsync(parcelTimeline);
        Assert.DoesNotContain("value=\"weather\"", parcelPage, StringComparison.Ordinal);
        Assert.Contains("Choose a cause the company offers.", WebUtility.HtmlDecode((await owner.PostAsync(parcelTimeline, parcelPage, ("key", Key()), ("eventKind", "Delay"), ("cause", "weather"))).Html),
            StringComparison.Ordinal);
        Assert.Equal(parcelTimeline + "?done=recorded", (await owner.PostAsync(parcelTimeline, parcelPage, ("key", Key()), ("eventKind", "Exception"), ("cause", own))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Exception: Wrong size declared", WebUtility.HtmlDecode((await owner.GetAsync(parcelTimeline)).Html), StringComparison.Ordinal);

        // The delay voided with a reason: its copy on the parcel goes too; a copy cannot be voided alone.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Guid copyId = (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().TrackingEvents.AsNoTracking().SingleAsync(item => item.CausedByEventId == delayId)).Id;
            Assert.Contains("Only an event recorded by hand here can be voided.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/events/void/" + copyId, page, ("reason", "x"))).Html),
                StringComparison.Ordinal);
        }

        Assert.Contains("Say why it is voided.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/events/void/" + delayId, page, ("reason", " "))).Html), StringComparison.Ordinal);
        Assert.Equal(timeline + "?done=voided", (await owner.PostAsync("/staff/events/void/" + delayId, page, ("reason", "Wrong shipment"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(timeline + "?done=voided", (await owner.PostAsync("/staff/events/void/" + delayId, page, ("reason", "Wrong shipment"))).Response.Headers.Location?.OriginalString);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Assert.Equal(2, await context.TrackingEvents.AsNoTracking().CountAsync(item => item.Code == "ops.tracking.event_voided"));
        }

        // Someone without the right sees nothing.
        using CookieClient worker = await PersonAsync(owner, "eventco", "Bundler", "bundler.events", "ops.bundles.fill");
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.PostAsync("/staff/events", (await worker.GetAsync("/staff/events")).Html, ("query", number))).Html)
            .ToLowerInvariant(), StringComparison.Ordinal);
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.GetAsync(timeline)).Html).ToLowerInvariant(), StringComparison.Ordinal);
    }

    // Slice 5F step 4: cards through the company's own Stripe account: the keys checked and never shown again, a reader, Stripe's payment page as a QR code,
    // paid only on Stripe's signed word (once), a reader payment checked against Stripe; cards turned off.
    [SqlServerFact]
    public async Task Cards_ConnectStripe_PayOnThePageOrAReader_OnlyOnStripesWord()
    {
        Guid tenant = NewTenant("cardco", "Card Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("cardco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.card@example.com");
        Guid destination;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        // Settings → Payments: wrong keys refused (shape, then Stripe); the right ones kept, only their last characters shown; the webhook's address given.
        const string secret = "sk_test_good51234567890ABCD";
        const string webhookSecret = "whsec_test_0123456789abcdef0123";
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/payments");
        Assert.Contains("https://cardco.techcrafters.ltd/hooks/stripe", settingsPage, StringComparison.Ordinal);
        string connect = "/staff/settings/payments/card/connect";
        Assert.Contains("card=ops.cards.keys_invalid", (await owner.PostAsync(connect, settingsPage, ("secretKey", "sk_live_short"), ("publishableKey", "pk_test_x"),
            ("webhookSecret", webhookSecret))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Contains("card=ops.cards.keys_invalid", (await owner.PostAsync(connect, settingsPage, ("secretKey", "sk_test_wrong1234567890ABCD"),
            ("publishableKey", "pk_test_0123456789abcdefghij"), ("webhookSecret", webhookSecret))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal("/staff/settings/payments?done=connected#cards", (await owner.PostAsync(connect, settingsPage, ("secretKey", secret),
            ("publishableKey", "pk_test_0123456789abcdefghij"), ("webhookSecret", webhookSecret))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string connected) = await owner.GetAsync("/staff/settings/payments");
        Assert.Contains("Cash Co Stripe", connected, StringComparison.Ordinal);
        Assert.Contains("ABCD", connected, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, connected, StringComparison.Ordinal);
        Assert.DoesNotContain(webhookSecret, connected, StringComparison.Ordinal);
        Assert.Equal("/staff/settings/payments?done=saved#cards", (await owner.PostAsync("/staff/settings/payments/card/readers", connected, ("warehouse", warehouse.ToString()),
            ("label", "Front desk"), ("reader", "tmr_FrontDesk01"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("card=ops.cards.reader_invalid", (await owner.PostAsync("/staff/settings/payments/card/readers", connected, ("warehouse", warehouse.ToString()),
            ("label", "Twice"), ("reader", "tmr_FrontDesk01"))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Domain.Billing.OperationsCardProvider kept = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().CardProviders.AsNoTracking().SingleAsync();
            Assert.DoesNotContain("good5", kept.SecretKeyProtected, StringComparison.Ordinal);
            Assert.DoesNotContain("0123456789abcdef", kept.WebhookSecretProtected, StringComparison.Ordinal);
        }

        // Two final shipments, each with its invoice.
        async Task<(Guid Id, decimal Owed)> InvoiceAsync(int nth)
        {
            string customerPath = "/staff/counter/customers/" + ana;
            (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
            (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
                ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"),
                ("recipient", "Jean Baptiste"), ("recipientPhone", "+50937000000"));
            string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
            (HttpResponseMessage _, string beforeParcel) = await owner.GetAsync(shipmentPath);
            await owner.PostAsync(shipmentPath + "/parcels", beforeParcel, ("key", FormKey(beforeParcel, shipmentPath + "/parcels")),
                ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Clothes"), ("value", "40"));
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Invoices.AsNoTracking()
                .OrderBy(item => item.CreatedUtc).ToListAsync())[nth];
            return (invoice.Id, invoice.Owed);
        }

        (Guid first, decimal owed) = await InvoiceAsync(0);
        string invoicePath = "/staff/cashier/invoices/" + first;
        (HttpResponseMessage _, string payPage) = await owner.GetAsync(invoicePath);
        Assert.Contains("Card: payment page (QR code)", WebUtility.HtmlDecode(payPage), StringComparison.Ordinal);
        Assert.Contains("Card on Front desk", WebUtility.HtmlDecode(payPage), StringComparison.Ordinal);
        Assert.DoesNotContain("<option value=\"Card\"", payPage, StringComparison.Ordinal);

        // Stripe's page: the QR code and its link; nothing is paid on the page's word.
        string chargePath = (await owner.PostAsync(invoicePath + "/card", payPage)).Response.Headers.Location!.OriginalString;
        Assert.Matches("^/staff/cashier/card/[0-9a-f-]{36}$", chargePath);
        Guid chargeId = Guid.Parse(chargePath[^36..]);
        string session = "cs_test_" + chargeId.ToString("N");
        Assert.Equal(owed, _host.Cards.Asked[session].Amount);
        Assert.Equal("https://cardco.techcrafters.ltd/staff/cashier/card/" + chargeId, _host.Cards.Asked[session].ReturnUrl);
        (HttpResponseMessage _, string waiting) = await owner.GetAsync(chargePath);
        Assert.Contains("data:image/svg+xml;base64,", waiting, StringComparison.Ordinal);
        Assert.Contains("https://checkout.stripe.com/c/pay/" + session, waiting, StringComparison.Ordinal);
        Assert.Contains("data-card-status=\"started\"", waiting, StringComparison.Ordinal);

        // Stripe's webhook: refused unsigned or signed with another secret; signed right, recorded once (sent twice).
        string Money(decimal amount) => (amount * 100m).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        string payload = "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"" + session + "\",\"payment_status\":\"paid\",\"status\":\"complete\","
            + "\"payment_intent\":\"pi_test_" + chargeId.ToString("N") + "\",\"amount_total\":" + Money(owed) + ",\"currency\":\"usd\",\"metadata\":{\"vixly_charge\":\"" + chargeId + "\"}}}}";

        async Task<HttpStatusCode> HookAsync(string body, string? key)
        {
            using HttpClient stripe = _host.Client("cardco.techcrafters.ltd");
            using HttpRequestMessage request = new(HttpMethod.Post, "/hooks/stripe") { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

            if (key is not null)
            {
                string stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
                byte[] mac = System.Security.Cryptography.HMACSHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key), System.Text.Encoding.UTF8.GetBytes(stamp + "." + body));
                request.Headers.Add("Stripe-Signature", "t=" + stamp + ",v1=" + Convert.ToHexStringLower(mac));
            }

            return (await stripe.SendAsync(request)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.BadRequest, await HookAsync(payload, null));
        Assert.Equal(HttpStatusCode.BadRequest, await HookAsync(payload, "whsec_someone_else_0123456789"));
        Assert.Contains("data-card-status=\"started\"", (await owner.GetAsync(chargePath)).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, await HookAsync(payload, webhookSecret));
        Assert.Equal(HttpStatusCode.OK, await HookAsync(payload, webhookSecret));
        string paidPage = WebUtility.HtmlDecode((await owner.GetAsync(chargePath)).Html);
        Assert.Contains("Paid by card: Stripe confirmed it.", paidPage, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsPayment payment = await context.Payments.AsNoTracking().SingleAsync(item => item.InvoiceId == first);
            Assert.Equal(Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Card, Assert.Single(payment.Tenders).Method);
            Assert.Equal("pi_test_" + chargeId.ToString("N"), payment.Tenders[0].Reference);
            Assert.Equal(0m, (await context.Invoices.AsNoTracking().SingleAsync(item => item.Id == first)).Owed);
        }

        // A reader: Stripe asked to take the card on it; paid there, the check against Stripe records it (no webhook needed).
        (Guid second, decimal secondOwed) = await InvoiceAsync(1);
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync("/staff/cashier/invoices/" + second);
        Guid readerId = Guid.Parse(Regex.Match(secondPage, "name=\"reader\" value=\"(?<id>[0-9a-f-]{36})\"").Groups["id"].Value);
        string readerPath = (await owner.PostAsync("/staff/cashier/invoices/" + second + "/card", secondPage, ("reader", readerId.ToString()))).Response.Headers.Location!.OriginalString;
        Guid readerCharge = Guid.Parse(readerPath[^36..]);
        Assert.Contains("process:tmr_FrontDesk01", _host.Cards.ReaderCalls);
        Assert.Contains("Waiting for the card on Front desk", WebUtility.HtmlDecode((await owner.GetAsync(readerPath)).Html), StringComparison.Ordinal);
        _host.Cards.Pay("pi_test_" + readerCharge.ToString("N"));
        Assert.Contains("Paid by card", WebUtility.HtmlDecode((await owner.GetAsync(readerPath)).Html), StringComparison.Ordinal);
        Assert.Equal(secondOwed, _host.Cards.Asked["pi_test_" + readerCharge.ToString("N")].Amount);

        // Second review: a card refund Stripe does not answer stays pending (the amount reserved) and is finished from the invoice, once at Stripe.
        using CookieClient finance = await PersonAsync(owner, "cardco", "Finance", "fin", "ops.invoices.view", "ops.credits.approve");
        string creditPage = (await owner.GetAsync("/staff/invoices/" + first)).Html;
        string line = Regex.Match(creditPage, "name=\"amount_(?<id>[0-9a-f]{32})\"").Groups["id"].Value;
        await owner.PostAsync("/staff/invoices/" + first + "/credits", creditPage, ("key", Key()), ("amount_" + line, "1.00"), ("reason", "Goodwill"));
        (HttpResponseMessage _, string toApprove) = await finance.GetAsync("/staff/invoices/credits");
        await finance.PostAsync(Regex.Match(toApprove, "/staff/invoices/credits/[0-9a-f-]{36}/decide").Value, toApprove, ("approve", "true"), ("back", "credits"));
        string refundable = (await owner.GetAsync("/staff/invoices/" + first)).Html;
        _host.Cards.RefundUnavailableOnce = true;
        Assert.Contains("why=ops.cards.provider_unavailable", (await owner.PostAsync("/staff/invoices/" + first + "/refunds", refundable, ("key", Key()), ("method", "Card"),
            ("amount", "1.00"), ("reason", "Goodwill"))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        string pendingPage = (await owner.GetAsync("/staff/invoices/" + first)).Html;
        Assert.Contains("data-refund-pending", pendingPage, StringComparison.Ordinal);
        Assert.Empty(_host.Cards.Refunds);
        string pendingKey = Regex.Match(pendingPage, "data-refund-pending>.*?name=\"key\" value=\"(?<key>[0-9a-f]{32})\"", RegexOptions.Singleline).Groups["key"].Value;
        Assert.Equal("/staff/invoices/" + first + "?done=refunded", (await owner.PostAsync("/staff/invoices/" + first + "/refunds", pendingPage, ("key", pendingKey), ("method", "Card"),
            ("amount", "1.00"), ("reason", "Goodwill"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(("pi_test_" + chargeId.ToString("N"), 1.00m), Assert.Single(_host.Cards.Refunds));
        Assert.DoesNotContain("data-refund-pending", (await owner.GetAsync("/staff/invoices/" + first)).Html, StringComparison.Ordinal);

        // Review: a cancelled page is expired at Stripe first; one Stripe already took is recorded as paid instead of cancelled.
        (Guid third, decimal _) = await InvoiceAsync(2);
        (HttpResponseMessage _, string thirdPage) = await owner.GetAsync("/staff/cashier/invoices/" + third);
        string thirdPath = (await owner.PostAsync("/staff/cashier/invoices/" + third + "/card", thirdPage)).Response.Headers.Location!.OriginalString;
        string thirdSession = "cs_test_" + thirdPath[^36..].Replace("-", string.Empty, StringComparison.Ordinal);
        _host.Cards.Pay(thirdSession);
        await owner.PostAsync(thirdPath + "/cancel", (await owner.GetAsync(thirdPath)).Html);
        Assert.Contains("Paid by card", WebUtility.HtmlDecode((await owner.GetAsync(thirdPath)).Html), StringComparison.Ordinal);

        // Review: disconnecting closes at Stripe what is still open, so nothing is paid on an account no longer read.
        (Guid fourth, decimal _) = await InvoiceAsync(3);
        (HttpResponseMessage _, string fourthPage) = await owner.GetAsync("/staff/cashier/invoices/" + fourth);
        string fourthPath = (await owner.PostAsync("/staff/cashier/invoices/" + fourth + "/card", fourthPage)).Response.Headers.Location!.OriginalString;
        string fourthSession = "cs_test_" + fourthPath[^36..].Replace("-", string.Empty, StringComparison.Ordinal);
        Assert.False(_host.Cards.Payments[fourthSession].Over);

        // Cards turned off: not offered at the cashier.
        (HttpResponseMessage _, string again) = await owner.GetAsync("/staff/settings/payments");
        await owner.PostAsync("/staff/settings/payments/card/accept", again, ("accept", "false"));
        Assert.DoesNotContain("Card: payment page", WebUtility.HtmlDecode((await owner.GetAsync("/staff/cashier/invoices/" + second)).Html), StringComparison.Ordinal);

        (HttpResponseMessage _, string beforeDisconnect) = await owner.GetAsync("/staff/settings/payments");
        Assert.Equal("/staff/settings/payments?done=disconnected#cards", (await owner.PostAsync("/staff/settings/payments/card/disconnect", beforeDisconnect)).Response.Headers.Location?.OriginalString);
        Assert.True(_host.Cards.Payments[fourthSession].Over);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Assert.False(await context.CardCharges.AnyAsync(item => item.Status == Vixly.Operations.Domain.Billing.OperationsCardChargeStatus.Started));
            Assert.Equal(0m, (await context.Invoices.AsNoTracking().SingleAsync(item => item.Id == third)).Owed);
        }
    }

    // Slice 5F step 2: the cashier: payment methods, a drawer, cash with change, Zelle verified by finance, a refund after a credit note, the drawer closed with its difference.
    [SqlServerFact]
    public async Task Cashier_TakesPaymentsInFull_ZelleWaitsForFinance_AndDrawersBalance()
    {
        Guid tenant = NewTenant("cashco", "Cash Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("cashco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana.cash@example.com");
        Guid destination;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            await context.SaveChangesAsync();
            destination = pickup.Id;
        }

        // Settings → Payments: Zelle needs its details and terms; then cash, Zelle, cheque and MonCash.
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/payments");
        Assert.Contains("Zelle needs a Zelle tag", WebUtility.HtmlDecode((await owner.PostAsync("/staff/settings/payments", settingsPage, ("cash", "true"), ("zelle", "true"))).Html),
            StringComparison.Ordinal);
        Assert.Equal("/staff/settings/payments?done=1", (await owner.PostFilesAsync("/staff/settings/payments", settingsPage, new[] { ("qr", "zelle.png", Png()) }, ("cash", "true"),
            ("zelle", "true"), ("cheque", "true"), ("other1", "MonCash"), ("zelleTag", "@cashco"), ("zelleName", "Cash Co LLC"),
            ("termsEn", "Write the shipment number in the memo. Your shipment waits until we see the money."))).Response.Headers.Location?.OriginalString);
        Assert.Equal("image/png", (await owner.GetAsync("/staff/settings/payments/zelle-qr")).Response.Content.Headers.ContentType?.MediaType);

        // A final shipment and its invoice.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"),
            ("recipientPhone", "+50937000000"));
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        (HttpResponseMessage _, string beforeParcel) = await owner.GetAsync(shipmentPath);
        await owner.PostAsync(shipmentPath + "/parcels", beforeParcel, ("key", FormKey(beforeParcel, shipmentPath + "/parcels")),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Clothes"), ("value", "40"));
        Guid invoiceId;
        decimal owed;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Invoices.SingleAsync();
            (invoiceId, owed) = (invoice.Id, invoice.Total);
        }

        string Money(decimal amount) => amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("Unpaid", WebUtility.HtmlDecode((await owner.GetAsync("/staff/shipments")).Html), StringComparison.Ordinal);

        // The cashier finds the customer by code; cash needs an open drawer.
        string cashier = WebUtility.HtmlDecode((await owner.GetAsync("/staff/cashier?find=" + anaCode)).Html);
        Assert.Contains("/staff/cashier/invoices/" + invoiceId, cashier, StringComparison.Ordinal);
        (HttpResponseMessage _, string payPage) = await owner.GetAsync("/staff/cashier/invoices/" + invoiceId);
        Assert.Contains("@cashco", payPage, StringComparison.Ordinal);
        Assert.Contains("The amounts must add up to exactly what is owed.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/cashier/invoices/" + invoiceId + "/pay", payPage,
            ("key", Key()), ("method_1", "Cheque"), ("amount_1", "1.00"))).Html), StringComparison.Ordinal);
        Assert.Contains("Open your cash drawer", WebUtility.HtmlDecode((await owner.PostAsync("/staff/cashier/invoices/" + invoiceId + "/pay", payPage,
            ("key", Key()), ("method_1", "Cash"), ("amount_1", Money(owed)))).Html), StringComparison.Ordinal);
        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        Assert.Equal("/staff/cashier?done=opened", (await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", warehouse.ToString()), ("float", "100"))).Response.Headers.Location?.OriginalString);

        // Paid in cash with 10.00 too much handed over: the change is shown; sent twice, taken once; the desk says paid.
        string payKey = Key();
        (HttpResponseMessage paid, string _) = await owner.PostAsync("/staff/cashier/invoices/" + invoiceId + "/pay", payPage, ("key", payKey), ("method_1", "Cash"), ("amount_1", Money(owed)),
            ("given_1", Money(owed + 10m)));
        string paymentPath = paid.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Matches("^/staff/cashier/payments/[0-9a-f-]{36}$", paymentPath);
        Assert.Equal(paymentPath, (await owner.PostAsync("/staff/cashier/invoices/" + invoiceId + "/pay", payPage, ("key", payKey), ("method_1", "Cash"), ("amount_1", Money(owed)))).Response
            .Headers.Location?.OriginalString);
        Assert.Contains("Change to give: 10.00 USD", WebUtility.HtmlDecode((await owner.GetAsync(paymentPath)).Html), StringComparison.Ordinal);
        Assert.Equal("application/pdf", (await owner.GetAsync(paymentPath + "/receipt")).Response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(">Paid<", WebUtility.HtmlDecode((await owner.GetAsync("/staff/shipments")).Html).Replace(" Paid", "Paid", StringComparison.Ordinal), StringComparison.Ordinal);

        // A credit note of 2.00 approved by finance is given back in cash from the owner's drawer.
        using CookieClient finance = await PersonAsync(owner, "cashco", "Finance", "fin", "ops.invoices.view", "ops.credits.approve", "ops.payments.confirm");
        string invoicePage = (await owner.GetAsync("/staff/invoices/" + invoiceId)).Html;
        string freightLine = Regex.Match(invoicePage, "name=\"amount_(?<id>[0-9a-f]{32})\"").Groups["id"].Value;
        await owner.PostAsync("/staff/invoices/" + invoiceId + "/credits", invoicePage, ("key", Key()), ("amount_" + freightLine, "2.00"), ("reason", "Goodwill"));
        (HttpResponseMessage _, string toApprove) = await finance.GetAsync("/staff/invoices/credits");
        await finance.PostAsync(Regex.Match(toApprove, "/staff/invoices/credits/[0-9a-f-]{36}/decide").Value, toApprove, ("approve", "true"), ("back", "credits"));
        string refundable = (await owner.GetAsync("/staff/invoices/" + invoiceId)).Html;
        Assert.Contains("/staff/invoices/" + invoiceId + "/refunds", refundable, StringComparison.Ordinal);
        Assert.Equal("/staff/invoices/" + invoiceId + "?done=refunded", (await owner.PostAsync("/staff/invoices/" + invoiceId + "/refunds", refundable, ("key", Key()), ("method", "Cash"),
            ("amount", "2.00"), ("reason", "Goodwill"))).Response.Headers.Location?.OriginalString);

        // An order paid by Zelle waits for finance; not received, it is owed again; then paid by MonCash.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            context.Add(Vixly.Operations.Domain.Supplies.OperationsSupply.Create(tenant, new Vixly.Operations.Domain.Supplies.OperationsSupplyDetails("BOX", "Box", null, null, null,
                Vixly.Operations.Domain.Supplies.OperationsSupplyCategory.Boxes, true), new[] { "0777" }, Vixly.Operations.Domain.Supplies.OperationsSupplyPrice.Create(5m, null, null, now), now));
            await context.SaveChangesAsync();
        }

        (HttpResponseMessage started, string _) = await owner.PostAsync(customerPath + "/orders", customerPage);
        string orderPath = started.Headers.Location?.OriginalString ?? string.Empty;
        (HttpResponseMessage _, string order) = await owner.GetAsync(orderPath);
        await owner.PostAsync(orderPath + "/supplies", order, ("key", Key()), ("scan", "0777"));
        await owner.PostAsync(orderPath + "/confirm", order);
        Guid orderInvoice;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            orderInvoice = (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Invoices.SingleAsync(item => item.Kind == Vixly.Operations.Domain.Billing.OperationsInvoiceKind.SupplyOrder)).Id;
        }

        (HttpResponseMessage _, string orderPay) = await owner.GetAsync("/staff/cashier/invoices/" + orderInvoice);
        Assert.Contains("The customer must accept the Zelle terms first.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/cashier/invoices/" + orderInvoice + "/pay", orderPay,
            ("key", Key()), ("method_1", "Zelle"), ("amount_1", "5.00"))).Html), StringComparison.Ordinal);
        Assert.Matches("^/staff/cashier/payments/", (await owner.PostAsync("/staff/cashier/invoices/" + orderInvoice + "/pay", orderPay, ("key", Key()), ("method_1", "Zelle"),
            ("amount_1", "5.00"), ("reference_1", "ZL-55"), ("zelleTerms", "true"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Awaiting confirmation", WebUtility.HtmlDecode((await owner.GetAsync("/staff/invoices/" + orderInvoice)).Html), StringComparison.Ordinal);
        (HttpResponseMessage _, string verify) = await finance.GetAsync("/staff/cashier/verify");
        Assert.Contains("ZL-55", verify, StringComparison.Ordinal);
        string check = Regex.Match(verify, "/staff/cashier/verify/[0-9a-f-]{36}/[0-9a-f-]{36}").Value;
        // The one who took it never confirms it (P-6).
        (HttpResponseMessage _, string ownVerify) = await owner.GetAsync("/staff/cashier/verify");
        Assert.Contains("never the one who took the payment", WebUtility.HtmlDecode((await owner.PostAsync(check, ownVerify, ("received", "true"), ("note", "Seen"))).Html), StringComparison.Ordinal);
        Assert.Contains("Give a reason.", WebUtility.HtmlDecode((await finance.PostAsync(check, verify, ("received", "false"))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/cashier/verify?done=not_received", (await finance.PostAsync(check, verify, ("received", "false"), ("note", "Nothing in the bank"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string payAgain) = await owner.GetAsync("/staff/cashier/invoices/" + orderInvoice);
        Assert.Matches("^/staff/cashier/payments/", (await owner.PostAsync("/staff/cashier/invoices/" + orderInvoice + "/pay", payAgain, ("key", Key()), ("method_1", "Other1"),
            ("amount_1", "5.00"))).Response.Headers.Location?.OriginalString);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsInvoice orderInvoiceRow = await context.Invoices.SingleAsync(item => item.Id == orderInvoice);
            Assert.Equal((Vixly.Operations.Domain.Billing.OperationsPaymentState.Paid, 5m, 0m), (orderInvoiceRow.PaymentState, orderInvoiceRow.PaidTotal, orderInvoiceRow.PendingTotal));
            Assert.Equal(2m, (await context.Invoices.SingleAsync(item => item.Id == invoiceId)).RefundedTotal);
            Assert.Single(await context.OutboxMessages.Where(message => message.Type == Vixly.Operations.Application.Billing.OperationsPaymentNotice.MessageType).ToListAsync());
        }

        // The drawer: 100 float + the cash - 2.00 refunded; counted 1.00 short needs a note, then it is closed with its difference.
        (HttpResponseMessage _, string drawerList) = await owner.GetAsync("/staff/cashier/drawers");
        string drawerPath = Regex.Match(drawerList, "/staff/cashier/drawers/[0-9a-f-]{36}").Value;
        (HttpResponseMessage _, string report) = await owner.GetAsync(drawerPath);
        decimal expected = 100m + owed - 2m;
        Assert.Contains("Count the cash", WebUtility.HtmlDecode(report), StringComparison.Ordinal);
        Assert.Contains("Give a reason.", WebUtility.HtmlDecode((await owner.PostAsync(drawerPath + "/close", report, ("counted", Money(expected - 1m)))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/cashier/drawers?done=closed", (await owner.PostAsync(drawerPath + "/close", report, ("counted", Money(expected - 1m)), ("note", "One dollar short")))
            .Response.Headers.Location?.OriginalString);
        Assert.Contains("One dollar short", (await finance.GetAsync(drawerPath)).Html, StringComparison.Ordinal);
    }

    // A staff member with a role of these permissions for the whole company.
    private async Task<CookieClient> PersonAsync(CookieClient owner, string slug, string roleName, string mailbox, params string[] permissions)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, new[] { ("name", roleName) }.Concat(permissions.Select(permission => ("permissions", permission))).ToArray());
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">" + Regex.Escape(roleName) + "</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", roleName + " Person"), ("email", mailbox + "@" + slug + ".example"), ("language", "en"), ("roleId", role),
            ("scope", "tenant"));
        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient person = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await person.GetAsync(new Uri(link).PathAndQuery);
        await person.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(person);
        return person;
    }

    private static string ShipmentId(string shipmentPath) => shipmentPath[(shipmentPath.LastIndexOf('/') + 1)..];

    // Slice 5F step 3: a customer screen paired by a one-use code; the clerk sends an invoice (Zelle terms accepted by the customer there count at the
    // cashier; card on Stripe's page) or a declaration to sign; a screen that changed refuses an old choice; removed, it stops at once.
    [SqlServerFact]
    public async Task CustomerScreen_PairedByCode_PaysAndSigns_OnlyWhatTheClerkSent()
    {
        const string host = "screenco.techcrafters.ltd";
        Guid tenant = NewTenant("screenco", "Screen Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("screenco");
        using CookieClient _owner = owner;
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.screen@example.com");
        Guid destination;
        Guid policy;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
            OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
            OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
                OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
            context.AddRange(haiti, pickup);
            context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
                new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
            // Slice 5F step 3: an insurance product with its policy's own words, read and signed on the screen.
            Vixly.Operations.Domain.Charges.OperationsInsurancePlan insured = Vixly.Operations.Domain.Charges.OperationsInsurancePlan.Create(tenant, Vixly.Operations.Domain.Charges.OperationsTransportMode.Air,
                new Vixly.Operations.Domain.Charges.OperationsInsuranceTerms(Vixly.Operations.Domain.Charges.OperationsInsuranceMethod.Percent, 2m, 5m,
                    Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsuranceBand>(), null,
                    Vixly.Operations.Domain.Charges.OperationsInsuranceCoverage.Defaults with { TermsEn = "Claims are paid within 30 days of proof of loss." }), null, now, "Full value");
            context.Add(insured);
            await context.SaveChangesAsync();
            destination = pickup.Id;
            policy = insured.Id;
        }

        // The company takes Zelle (with its QR picture and terms) and cards.
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/payments");
        await owner.PostFilesAsync("/staff/settings/payments", settingsPage, new[] { ("qr", "zelle.png", Png()) }, ("cash", "true"), ("zelle", "true"), ("zelleTag", "@screenco"),
            ("zelleName", "Screen Co LLC"), ("termsEn", "Write the shipment number in the memo."));
        (HttpResponseMessage _, string cardPage) = await owner.GetAsync("/staff/settings/payments");
        Assert.Equal("/staff/settings/payments?done=connected#cards", (await owner.PostAsync("/staff/settings/payments/card/connect", cardPage, ("secretKey", "sk_test_good51234567890SCRN"),
            ("publishableKey", "pk_test_0123456789abcdefghij"), ("webhookSecret", "whsec_test_0123456789abcdef0123"))).Response.Headers.Location?.OriginalString);

        async Task<(Guid Shipment, string Path)> ShipmentAsync(int parcels)
        {
            string customerPath = "/staff/counter/customers/" + ana;
            (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
            (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
                ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", parcels.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("recipient", "Jean Baptiste"), ("recipientPhone", "+50937000000"));
            string path = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
            (HttpResponseMessage _, string beforeParcel) = await owner.GetAsync(path);
            await owner.PostAsync(path + "/parcels", beforeParcel, ("key", FormKey(beforeParcel, path + "/parcels")),
                ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10"), ("contents", "Clothes"), ("value", "40"));
            return (Guid.Parse(path[^36..]), path);
        }

        async Task<Vixly.Operations.Domain.Billing.OperationsInvoice> InvoiceOfAsync(Guid shipment)
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Invoices.AsNoTracking().SingleAsync(item => item.ShipmentId == shipment);
        }

        // A screen added at the warehouse: its code, shown once; a wrong code refused; the right one pairs the tablet, once.
        (HttpResponseMessage _, string screensPage) = await owner.GetAsync("/staff/screens");
        string added = (await owner.PostAsync("/staff/screens", screensPage, ("warehouse", warehouse.ToString()), ("name", "Front tablet"))).Html;
        string code = Regex.Match(added, "data-code>(?<code>[A-Z0-9-]{11})<").Groups["code"].Value;
        Assert.Matches("^[A-Z0-9]{5}-[A-Z0-9]{5}$", code);
        Guid screenId;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Domain.Screens.OperationsCustomerScreen kept = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().CustomerScreens.AsNoTracking().SingleAsync();
            Assert.DoesNotContain(code.Replace("-", string.Empty, StringComparison.Ordinal), kept.PairingCodeHash ?? string.Empty, StringComparison.Ordinal);
            screenId = kept.Id;
        }

        using CookieClient tablet = _host.Browser(host);
        (HttpResponseMessage _, string pairPage) = await tablet.GetAsync("/screen");
        Assert.Contains("action=\"/screen/pair\"", pairPage, StringComparison.Ordinal);
        Assert.Equal("/screen?problem=ops.screens.code_invalid", (await tablet.PostAsync("/screen/pair", pairPage, ("code", "AAAAA-AAAAA"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/screen", (await tablet.PostAsync("/screen/pair", pairPage, ("code", code.ToLowerInvariant()))).Response.Headers.Location?.OriginalString);
        Assert.Contains("data-screen-step=\"idle\"", (await tablet.GetAsync("/screen")).Html, StringComparison.Ordinal);
        using CookieClient another = _host.Browser(host);
        (HttpResponseMessage _, string anotherPage) = await another.GetAsync("/screen");
        Assert.Contains("problem=ops.screens.code_invalid", (await another.PostAsync("/screen/pair", anotherPage, ("code", code))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, (await another.GetAsync("/screen/version")).Response.StatusCode);

        // The clerk sends the invoice; the screen shows it with card and Zelle (never cash).
        (Guid firstShipment, string _) = await ShipmentAsync(1);
        Vixly.Operations.Domain.Billing.OperationsInvoice first = await InvoiceOfAsync(firstShipment);
        string invoicePath = "/staff/cashier/invoices/" + first.Id;
        (HttpResponseMessage _, string payPage) = await owner.GetAsync(invoicePath);
        Assert.Contains("data-screens", payPage, StringComparison.Ordinal);
        Assert.Equal(invoicePath + "?screen=sent#screens", (await owner.PostAsync("/staff/screens/" + screenId + "/send", payPage, ("invoice", first.Id.ToString()),
            ("back", "invoice"))).Response.Headers.Location?.OriginalString);

        async Task<(string Html, string Version)> ScreenAsync()
        {
            string html = WebUtility.HtmlDecode((await tablet.GetAsync("/screen")).Html);
            return (html, Regex.Match(html, "data-screen-version=\"(?<v>[^\"]+)\"").Groups["v"].Value);
        }

        Task<(HttpResponseMessage Response, string Html)> ActAsync(string html, string action, string version, params (string, string)[] more) =>
            tablet.PostAsync("/screen/act", html, new[] { ("action", action), ("version", version) }.Concat(more).ToArray());

        (string summary, string version) = await ScreenAsync();
        Assert.Contains("data-screen-step=\"summary\"", summary, StringComparison.Ordinal);
        Assert.Contains("Pay by card", summary, StringComparison.Ordinal);
        Assert.Contains("Pay with Zelle", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"Cash\"", summary, StringComparison.Ordinal);
        Assert.Equal(version, (await tablet.GetAsync("/screen/version")).Html);

        // Zelle: the warning and terms first; the QR picture only once they are accepted; an old choice refused.
        Assert.Equal("/screen", (await ActAsync(summary, "Zelle", version)).Response.Headers.Location?.OriginalString);
        (string terms, string termsVersion) = await ScreenAsync();
        Assert.Contains("Write the shipment number in the memo.", terms, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await tablet.GetAsync("/screen/zelle-qr")).Response.StatusCode);
        Assert.Contains("problem=ops.screens.step_invalid", (await ActAsync(terms, "Back", version)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        await ActAsync(terms, "AcceptZelle", termsVersion);
        (string zelle, string _) = await ScreenAsync();
        Assert.Contains("data-memo>" + first.Reference + "<", zelle, StringComparison.Ordinal);
        Assert.Contains("@screenco", zelle, StringComparison.Ordinal);
        Assert.Equal("image/png", (await tablet.GetAsync("/screen/zelle-qr")).Response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("data-zelle-accepted", (await owner.GetAsync(invoicePath)).Html, StringComparison.Ordinal);

        // The cashier records the Zelle payment: the customer's acceptance on the screen counts (no tick by the clerk); the screen says paid.
        (HttpResponseMessage _, string toPay) = await owner.GetAsync(invoicePath);
        Assert.Matches("^/staff/cashier/payments/", (await owner.PostAsync(invoicePath + "/pay", toPay, ("key", Key()), ("method_1", "Zelle"),
            ("amount_1", first.Owed.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)), ("reference_1", "ZL123"))).Response.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Contains("data-settled", (await ScreenAsync()).Html, StringComparison.Ordinal);

        // A second invoice paid by card on Stripe's page from the screen; Stripe's word moves the screen on.
        (Guid secondShipment, string _) = await ShipmentAsync(1);
        Vixly.Operations.Domain.Billing.OperationsInvoice second = await InvoiceOfAsync(secondShipment);
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync("/staff/cashier/invoices/" + second.Id);
        await owner.PostAsync("/staff/screens/" + screenId + "/send", secondPage, ("invoice", second.Id.ToString()), ("back", "invoice"));
        (string secondSummary, string secondVersion) = await ScreenAsync();
        Assert.Equal("/screen", (await ActAsync(secondSummary, "Card", secondVersion)).Response.Headers.Location?.OriginalString);
        (string card, string _) = await ScreenAsync();
        string session = Regex.Match(card, "https://checkout.stripe.com/c/pay/(?<id>cs_test_[0-9a-f]{32})").Groups["id"].Value;
        Assert.NotEmpty(session);
        Assert.Equal("https://" + host + "/screen?from=stripe", _host.Cards.Asked[session].ReturnUrl);
        Assert.Equal(second.Owed, _host.Cards.Asked[session].Amount);
        _host.Cards.Pay(session);
        Assert.Contains("Paid by card. Thank you!", (await ScreenAsync()).Html, StringComparison.Ordinal);
        Assert.Contains("data-screen-step=\"done\"", (await ScreenAsync()).Html, StringComparison.Ordinal);
        Assert.Contains("data-from-stripe", (await another.GetAsync("/screen?from=stripe")).Html, StringComparison.Ordinal);

        // The declaration of an open shipment, signed on the screen by the customer: the shipment's own signature, with the clerk who sent it.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (HttpResponseMessage open, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", warehouse.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("service", "air_standard"), ("parcelCount", "2"),
            ("recipient", "Jean Baptiste"), ("recipientPhone", "+50937000000"), ("insurance", policy.ToString()), ("insuranceAccepted", "true"));
        string openPath = Regex.Match(open.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;
        Guid openShipment = Guid.Parse(openPath[^36..]);
        (HttpResponseMessage _, string openPage) = await owner.GetAsync(openPath);
        await owner.PostAsync(openPath + "/parcels", openPage, ("key", FormKey(openPage, openPath + "/parcels")),
            ("weight", "1.5"), ("length", "20"), ("width", "20"), ("height", "10"), ("contents", "Shoes"), ("value", "60"));
        (HttpResponseMessage _, string signPage) = await owner.GetAsync(openPath);
        Assert.Equal(openPath + "?screen=sent#screens", (await owner.PostAsync("/staff/screens/" + screenId + "/send", signPage, ("shipment", openShipment.ToString()),
            ("back", "shipment"))).Response.Headers.Location?.OriginalString);
        (string declaration, string signVersion) = await ScreenAsync();
        Assert.Contains("data-screen-step=\"signature\"", declaration, StringComparison.Ordinal);
        Assert.Contains("Shoes", declaration, StringComparison.Ordinal);
        Assert.Contains("data-insured", declaration, StringComparison.Ordinal);
        Assert.Contains("Claims are paid within 30 days of proof of loss.", declaration, StringComparison.Ordinal);
        Assert.Contains("name=\"accept\"", declaration, StringComparison.Ordinal);
        Assert.Contains("problem=ops.screens.signature_required", (await ActAsync(declaration, "Sign", signVersion, ("accept", "true"))).Response.Headers.Location?.OriginalString,
            StringComparison.Ordinal);
        Assert.Contains("problem=ops.screens.accept_required", (await ActAsync(declaration, "Sign", signVersion,
            ("signature", "data:image/png;base64," + Convert.ToBase64String(Png())))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        // Review: the shipment changed after the customer read it (here, signed on the clerk's pad): the old page's signature is refused.
        string drawn = "data:image/png;base64," + Convert.ToBase64String(Png());
        (HttpResponseMessage _, string padPage) = await owner.GetAsync(openPath);
        await owner.PostAsync(openPath + "/signature", padPage, ("signature", drawn));
        Assert.Contains("problem=ops.screens.step_invalid", (await ActAsync(declaration, "Sign", signVersion, ("signature", drawn), ("accept", "true"))).Response.Headers.Location?.OriginalString,
            StringComparison.Ordinal);
        (declaration, signVersion) = await ScreenAsync();
        Assert.Equal("/screen", (await ActAsync(declaration, "Sign", signVersion, ("signature", drawn), ("accept", "true"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("data-screen-step=\"done\"", (await ScreenAsync()).Html, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Shipments.OperationsShipment signed = await context.Shipments.AsNoTracking().SingleAsync(item => item.Id == openShipment);
            Assert.NotNull(signed.CustomerSignatureKey);
            Assert.NotNull(signed.CustomerSignatureRecordedByUserId);
            Assert.True(await context.ScreenRecords.AnyAsync(record => record.InvoiceId == first.Id));
        }

        // Clear · next customer, then removed: the tablet is asked for a code again.
        (HttpResponseMessage _, string clearPage) = await owner.GetAsync(openPath);
        await owner.PostAsync("/staff/screens/" + screenId + "/send", clearPage, ("clear", "true"), ("shipment", openShipment.ToString()), ("back", "shipment"));
        Assert.Contains("data-screen-step=\"idle\"", (await ScreenAsync()).Html, StringComparison.Ordinal);
        (HttpResponseMessage _, string listPage) = await owner.GetAsync("/staff/screens");
        Assert.Equal("/staff/screens", (await owner.PostAsync("/staff/screens/" + screenId + "/remove", listPage)).Response.Headers.Location?.OriginalString);
        Assert.Contains("action=\"/screen/pair\"", (await tablet.GetAsync("/screen")).Html, StringComparison.Ordinal);
    }

    private static string Key() => Guid.NewGuid().ToString("N");

    // A front-desk clerk: serves at the counter, approves no discount.
    private async Task<CookieClient> ClerkAsync(CookieClient owner, string slug)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, ("name", "Front desk clerk"), ("permissions", "ops.counter.serve"));
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Front desk clerk</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", "Joe Clerk"), ("email", "joe@" + slug + ".example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));
        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient joe = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await joe.GetAsync(new Uri(link).PathAndQuery);
        await joe.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(joe);
        return joe;
    }

    // Owner, 2026-10-20: the counter is for the people given it; a website editor does not see customers there.
    [SqlServerFact]
    public async Task Counter_IsOnlyForThoseServingAtTheCounter()
    {
        Guid tenant = NewTenant("counterno", "Counter No");
        using CookieClient owner = await OwnerAsync("counterno");
        await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.no@example.com");
        using CookieClient ada = await StaffAsync(owner, "counterno");

        (HttpResponseMessage search, string page) = await ada.GetAsync("/staff/counter?find=37123456");
        Assert.Null(search.Headers.Location);
        Assert.DoesNotContain("ana.no@example.com", page, StringComparison.Ordinal);
        (HttpResponseMessage _, string customer) = await ada.GetAsync("/staff/counter/customers/" + ana);
        Assert.DoesNotContain("ana.no@example.com", customer, StringComparison.Ordinal);
        (HttpResponseMessage _, string home) = await ada.GetAsync("/staff");
        Assert.DoesNotContain("href=\"/staff/counter\"", home, StringComparison.Ordinal);
    }

    private static string FormKey(string html, string action) =>
        Regex.Match(html, "action=\"" + Regex.Escape(action) + "\"[\\s\\S]*?name=\"key\" value=\"(?<key>[^\"]+)\"").Groups["key"].Value;

    private async Task AgeAsync(Guid tenant, Guid parcel, int days)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        DateTimeOffset received = DateTimeOffset.UtcNow.AddDays(-days).AddMinutes(-5);
        await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.Where(item => item.Id == parcel)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedUtc, received));
    }

    private static Task<(HttpResponseMessage Response, string Html)> StationCheckAsync(CookieClient clerk, Guid parcel, string form, Guid shippingPrinter) =>
        clerk.PostFilesAsync("/staff/inspection/" + parcel, form, new[] { ("photo_package", "photo_package.png", Png()) },
            ("key", Regex.Match(form, "name=\"key\" value=\"(?<key>[0-9a-f]{32})\"").Groups["key"].Value), ("condition", "intact"),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10.5"), ("shippingPrinter", shippingPrinter.ToString()));

    [SqlServerFact]
    public async Task Clerk_PutsAway_PrintsTheLabel_AndSeesThePhotos()
    {
        Guid tenant = NewTenant("shelfco", "Shelf Co");
        using CookieClient owner = await OwnerAsync("shelfco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456785", "Books");

        (HttpResponseMessage _, string shelves) = await owner.GetAsync("/staff/locations/warehouse/" + warehouse + "/storage");
        (HttpResponseMessage added, string _) = await owner.PostAsync("/staff/locations/warehouse/" + warehouse + "/storage", shelves, ("code", "a-03-2"), ("zone", "storage"));
        Assert.Equal("/staff/locations/warehouse/" + warehouse + "/storage?done=saved", added.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string again) = await owner.PostAsync("/staff/locations/warehouse/" + warehouse + "/storage", shelves, ("code", "A-03-2"), ("zone", "storage"));
        Assert.Contains("already has a shelf with this code", again, StringComparison.Ordinal);

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        Guid parcel = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456785")).GetProperty("parcelId").GetGuid();
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/inspection/" + parcel);
        await CheckAsync(owner, "/staff/inspection/" + parcel, form, "intact", new[] { "photo_package" });

        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/parcels/" + parcel);
        string shelf = Regex.Match(page, "<option value=\"(?<id>[0-9a-f-]{36})\">A-03-2").Groups["id"].Value;
        (HttpResponseMessage putAway, string _) = await owner.PostAsync("/staff/parcels/" + parcel + "/put-away", page, ("location", shelf), ("key", NewKey()));
        Assert.Equal("/staff/parcels/" + parcel + "?done=put_away", putAway.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string after) = await owner.GetAsync("/staff/parcels/" + parcel);
        Assert.Contains("Put away at A-03-2", after, StringComparison.Ordinal);

        HttpResponseMessage label = (await owner.GetAsync("/staff/parcels/" + parcel + "/label")).Response;
        Assert.Equal("application/pdf", label.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(await label.Content.ReadAsByteArrayAsync())[..4], StringComparison.Ordinal);

        string photoPath = Regex.Match(after, "/staff/parcels/" + parcel + "/photos/[0-9a-f-]{36}").Value;
        HttpResponseMessage photo = (await owner.GetAsync(photoPath)).Response;
        Assert.Equal("image/png", photo.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", photo.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(Png(), await photo.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync("/staff/parcels/" + Guid.NewGuid() + "/photos/" + photoPath[^36..])).Response.StatusCode);
    }

    [SqlServerFact]
    public async Task DockPrinter_GetsALabelPerScan_ThroughTheCompanysPrintProgramOnly()
    {
        Guid tenant = NewTenant("printco", "Print Co");
        Guid otherTenant = NewTenant("otherprint", "Other Print");
        using CookieClient owner = await OwnerAsync("printco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        await PreAlertAsync(tenant, ana, "1Z999AA10123456784", "Shoes");
        string printers = "/staff/locations/warehouse/" + warehouse + "/printers";

        (HttpResponseMessage _, string empty) = await owner.GetAsync(printers);
        (HttpResponseMessage _, string badAddress) = await owner.PostAsync(printers + "/", empty, ("name", "Dock 1"), ("connection", "network"), ("address", "10.0.0.50:22"), ("dpi", "203"));
        Assert.Contains("role=\"alert\"", badAddress, StringComparison.Ordinal);
        (HttpResponseMessage saved, string _) = await owner.PostAsync(printers + "/", empty, ("name", "Dock 1"), ("connection", "network"), ("address", "10.0.0.50:9100"), ("dpi", "203"));
        Assert.Equal(printers + "?done=saved", saved.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string listed) = await owner.GetAsync(printers);
        Guid printer = Guid.Parse(Regex.Match(listed, "name=\"printer\" value=\"(?<id>[0-9a-f-]{36})\"").Groups["id"].Value);

        // The print program's key is shown once.
        (HttpResponseMessage _, string registered) = await owner.PostAsync(printers + "/agents", listed, ("name", "Dock PC 1"));
        string key = Regex.Match(registered, "<code>(?<key>vxpa\\.[^<]+)</code>").Groups["key"].Value;
        Assert.NotEmpty(key);
        (HttpResponseMessage _, string later) = await owner.GetAsync(printers);
        Assert.DoesNotContain(key, later, StringComparison.Ordinal);
        Assert.Contains("Dock PC 1", later, StringComparison.Ordinal);
        using HttpClient program = Agent("printco", key);
        Assert.Empty(await JobsAsync(program));

        // A test label, then a label per scan with the customer's code.
        (HttpResponseMessage tested, string _) = await owner.PostAsync(printers + "/" + printer + "/test", listed);
        Assert.Equal(printers + "?done=tested", tested.Headers.Location?.OriginalString);
        PrintJobDto test = Assert.Single(await JobsAsync(program));
        Assert.Equal("network", test.Connection);
        Assert.Equal("10.0.0.50:9100", test.Address);
        Assert.StartsWith("^XA", test.Payload, StringComparison.Ordinal);
        Assert.InRange(test.ExpiresInSeconds, 60, 120);
        Assert.Equal(HttpStatusCode.NoContent, (await program.PostAsJsonAsync("/agent/v1/print-jobs/" + test.JobId, new { printed = true })).StatusCode);
        Assert.Empty(await JobsAsync(program));

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        JsonElement scanned = await ScanOkAsync(owner, station, delivery, "1Z999AA10123456784", printer: printer);
        Assert.True(scanned.GetProperty("queued").GetBoolean());
        string number = scanned.GetProperty("number").GetString()!;
        PrintJobDto label = Assert.Single(await JobsAsync(program));
        Assert.Contains(number, label.Payload, StringComparison.Ordinal);
        Assert.Contains(anaCode, label.Payload, StringComparison.Ordinal);

        // A label is printed at most once: a job taken or failed is never handed out again, since it would come out on the wrong package.
        Assert.Empty(await JobsAsync(program));
        Assert.Equal(HttpStatusCode.NoContent, (await program.PostAsJsonAsync("/agent/v1/print-jobs/" + label.JobId, new { printed = false, errorCode = "paper_out" })).StatusCode);
        Assert.Empty(await JobsAsync(program));

        // A reprint from the parcel page goes to the chosen printer.
        Guid parcel = scanned.GetProperty("parcelId").GetGuid();
        (HttpResponseMessage _, string parcelPage) = await owner.GetAsync("/staff/parcels/" + parcel);
        (HttpResponseMessage printed, string _) = await owner.PostAsync("/staff/parcels/" + parcel + "/print", parcelPage, ("printer", printer.ToString()));
        Assert.Equal("/staff/parcels/" + parcel + "?done=printed", printed.Headers.Location?.OriginalString);
        Assert.Contains(number, Assert.Single(await JobsAsync(program)).Payload, StringComparison.Ordinal);

        // The key works only for its own company, and not once the program is stopped.
        using HttpClient elsewhere = Agent("otherprint", key);
        Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync("/agent/v1/print-jobs")).StatusCode);
        using HttpClient forged = Agent("printco", key[..^4] + "AAAA");
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync("/agent/v1/print-jobs")).StatusCode);
        using HttpClient anonymous = _host.Client("printco.techcrafters.ltd");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/agent/v1/print-jobs")).StatusCode);

        string agentId = Regex.Match(later, "/printers/agents/(?<id>[0-9a-f-]{36})/stop").Groups["id"].Value;
        (HttpResponseMessage stopped, string _) = await owner.PostAsync(printers + "/agents/" + agentId + "/stop", later);
        Assert.Equal(printers + "?done=stopped", stopped.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Unauthorized, (await program.GetAsync("/agent/v1/print-jobs")).StatusCode);
        Assert.NotEqual(Guid.Empty, otherTenant);
    }

    [SqlServerFact]
    public async Task StaffWithoutTheseRoles_CannotScanCheckOrSeeParcels()
    {
        Guid tenant = NewTenant("quietco", "Quiet Co");
        using CookieClient owner = await OwnerAsync("quietco");
        Guid warehouse = await WarehouseAsync(tenant);
        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        Guid parcel = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456786")).GetProperty("parcelId").GetGuid();

        using CookieClient ada = await StaffAsync(owner, "quietco");

        // What a role does not allow is not shown at all: not in the menu, not on the home page, not as a button (owner, 2026-10-18).
        (HttpResponseMessage _, string ownerHome) = await owner.GetAsync("/staff");
        Assert.Contains("href=\"/staff/settings\"", ownerHome, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/receiving\"", ownerHome, StringComparison.Ordinal);
        (HttpResponseMessage _, string adaHome) = await ada.GetAsync("/staff");
        Assert.Contains("href=\"/staff/site\"", adaHome, StringComparison.Ordinal);
        foreach (string hidden in new[] { "/staff/receiving", "/staff/inspection", "/staff/parcels", "/staff/shipments", "/staff/insurance", "/staff/customers", "/staff/locations", "/staff/settings", "/staff/people", "/staff/roles", "/staff/devices", "/staff/company", "/staff/audit" })
        {
            Assert.DoesNotContain("href=\"" + hidden + "\"", adaHome, StringComparison.Ordinal);
        }

        (HttpResponseMessage _, string adaSite) = await ada.GetAsync("/staff/site");
        Assert.DoesNotContain("action=\"/staff/site/publish\"", adaSite, StringComparison.Ordinal);
        Assert.Contains("type=\"submit\"", adaSite, StringComparison.Ordinal);
        (HttpResponseMessage _, string ownerSite) = await owner.GetAsync("/staff/site");
        Assert.Contains("action=\"/staff/site/publish\"", ownerSite, StringComparison.Ordinal);
        (HttpResponseMessage _, string adaStation) = await ada.GetAsync("/staff/scan");
        Assert.Contains("Your role does not allow this.", adaStation, StringComparison.Ordinal);
        Assert.DoesNotContain("data-step=\"deliveries\"", adaStation, StringComparison.Ordinal);
        (HttpResponseMessage open, JsonElement _) = await ada.PostJsonAsync("/staff/scan/api/deliveries", adaStation, new { warehouseId = warehouse, carrier = "UPS", key = NewKey() });
        Assert.Equal(HttpStatusCode.BadRequest, open.StatusCode);
        (HttpResponseMessage scan, JsonElement _) = await ScanAsync(ada, adaStation, delivery, "1Z999AA10123456787");
        Assert.Equal(HttpStatusCode.BadRequest, scan.StatusCode);
        (HttpResponseMessage close, JsonElement _) = await ada.PostJsonAsync("/staff/scan/api/deliveries/" + delivery + "/close", adaStation, new { });
        Assert.Equal(HttpStatusCode.BadRequest, close.StatusCode);

        (HttpResponseMessage _, string refusedList) = await ada.GetAsync("/staff/parcels");
        Assert.DoesNotContain("1Z999AA10123456786", refusedList, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.GetAsync("/staff/parcels/" + parcel)).Response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.GetAsync("/staff/parcels/" + parcel + "/label")).Response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.GetAsync("/staff/inspection/" + parcel)).Response.StatusCode);
        (HttpResponseMessage _, string insurance) = await ada.GetAsync("/staff/insurance");
        Assert.Contains("Your role does not allow this.", insurance, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.GetAsync("/staff/insurance/policies/new")).Response.StatusCode);
        (HttpResponseMessage _, string shipments) = await ada.GetAsync("/staff/shipments?view=all");
        Assert.Contains("Your role does not allow this.", shipments, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Forbidden, (await ada.GetAsync("/staff/shipments/export?view=all")).Response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.GetAsync("/staff/locations/warehouse/" + warehouse + "/printers")).Response.StatusCode);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        Assert.Equal(1, await context.Parcels.CountAsync());
        Assert.Equal(1, await context.InboundDeliveries.CountAsync());
    }

    // A double click that gets past the browser guard (two tabs, a slow page, no script): the same form twice at the same moment never fails (ADR-0019).
    [SqlServerFact]
    public async Task EveryForm_SentTwiceAtOnce_NeverFails()
    {
        Guid tenant = NewTenant("twiceco", "Twice Co");
        Guid platformTenant = _host.Platform.Portals["twiceco"].TenantId!.Value;
        await _host.InviteOwnerAsync(platformTenant);
        using CookieClient owner = _host.Browser("twiceco.techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains("twiceco", StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await owner.GetAsync(new Uri(link).PathAndQuery);
        string token = Uri.UnescapeDataString(link.Split("token=")[1]);
        List<string> failures = new();
        (HttpResponseMessage accepted, string _) = await TwiceAsync(failures, "invitation", () => owner.PostAsync("/invitation", invitation, ("token", token), ("password", Password)));
        (HttpResponseMessage _, string enrol) = await owner.GetAsync(accepted.Headers.Location?.OriginalString ?? "/staff/sign-in/enrol");
        string manualKey = ManualKey().Match(enrol).Groups["key"].Value;
        await TwiceAsync(failures, "enrol", () => owner.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(manualKey, DateTime.UtcNow))));
        (HttpResponseMessage _, string devicePage) = await owner.GetAsync("/devices/enrol");
        await TwiceAsync(failures, "device enrol", () => owner.PostAsync("/devices/enrol", devicePage, ("name", "Laptop"), ("kind", "pc")));

        // Sign out, then sign in again with two clicks on each step.
        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        await owner.PostAsync("/staff/sign-out", home);
        (HttpResponseMessage _, string signIn) = await owner.GetAsync("/staff");
        (HttpResponseMessage password, string _) = await TwiceAsync(failures, "sign-in", () => owner.PostAsync("/staff/sign-in", signIn, ("email", "grace@twiceco.example"), ("password", Password)));
        (HttpResponseMessage _, string verify) = await owner.GetAsync(password.Headers.Location?.OriginalString ?? "/staff/sign-in/verify");
        await TwiceAsync(failures, "verify", () => owner.PostAsync("/staff/sign-in/verify", verify, ("code", Code(manualKey, DateTime.UtcNow.AddSeconds(30)))));

        Guid warehouse = await WarehouseAsync(tenant);
        (HttpResponseMessage _, string roleForm) = await owner.GetAsync("/staff/roles/new");
        await TwiceAsync(failures, "role", () => owner.PostAsync("/staff/roles/save", roleForm, ("name", "Dock clerk"), ("permissions", "ops.receiving.receive")));
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Dock clerk</option>").Groups["id"].Value;
        await TwiceAsync(failures, "invite", () => owner.PostAsync("/staff/people/invite", people, ("fullName", "Ada Lovelace"), ("email", "ada@twiceco.example"), ("language", "en"), ("roleId", role), ("scope", "tenant")));
        (HttpResponseMessage _, string branchForm) = await owner.GetAsync("/staff/locations/branch");
        await TwiceAsync(failures, "branch", () => owner.PostAsync("/staff/locations/branch", branchForm, ("code", "PAP"), ("name", "Port-au-Prince office"), ("line1", "12 Rue Capois"),
            ("city", "Port-au-Prince"), ("countryCode", "HT"), ("phone", "+50922223333"), ("side", "Destination"), ("status", "Active"), ("showOnSite", "true")));
        (HttpResponseMessage _, string contactForm) = await owner.GetAsync("/staff/locations/contact");
        await TwiceAsync(failures, "contact", () => owner.PostAsync("/staff/locations/contact", contactForm, ("countryCode", "HT"), ("phones", "+50948605107"), ("sortOrder", "1")));
        (HttpResponseMessage _, string shelves) = await owner.GetAsync("/staff/locations/warehouse/" + warehouse + "/storage");
        await TwiceAsync(failures, "shelf", () => owner.PostAsync("/staff/locations/warehouse/" + warehouse + "/storage", shelves, ("code", "B-01"), ("zone", "storage")));
        (HttpResponseMessage _, string printers) = await owner.GetAsync("/staff/locations/warehouse/" + warehouse + "/printers");
        await TwiceAsync(failures, "printer", () => owner.PostAsync("/staff/locations/warehouse/" + warehouse + "/printers/", printers, ("name", "Dock 1"), ("connection", "network"), ("address", "10.0.0.5:9100"), ("dpi", "203")));
        await TwiceAsync(failures, "print program", () => owner.PostAsync("/staff/locations/warehouse/" + warehouse + "/printers/agents", printers, ("name", "Dock PC")));
        (HttpResponseMessage _, string settings) = await owner.GetAsync("/staff/settings");
        await TwiceAsync(failures, "settings", () => owner.PostAsync("/staff/settings", settings, ("currencyCode", "USD"), ("weightUnit", "Pound"), ("dimensionUnit", "Inch"), ("shipmentPrefix", "SH"),
            ("receivingPrefix", "WR"), ("invoicePrefix", "INV"), ("paymentPrefix", "PAY"), ("parcelPrefix", "PK")));
        (HttpResponseMessage _, string site) = await owner.GetAsync("/staff/site");
        await TwiceAsync(failures, "site", () => owner.PostAsync("/staff/site?language=en", site, ("displayName", "Twice Co")));
        await TwiceAsync(failures, "publish", () => owner.PostAsync("/staff/site/publish", site));
        (HttpResponseMessage _, string account) = await owner.GetAsync("/staff/account");
        await TwiceAsync(failures, "language", () => owner.PostAsync("/staff/account/language", account, ("language", "fr")));
        await owner.PostAsync("/staff/account/language", account, ("language", "en"));

        // Receiving: the scan app's calls, then the parcel page and the inspection station.
        (Guid ana, string anaCode) = await CustomerAsync(tenant, "ana@example.com");
        await PreAlertAsync(tenant, ana, "1Z999AA10123456784", "Shoes");
        (HttpResponseMessage _, string station) = await owner.GetAsync("/staff/scan?warehouse=" + warehouse);
        string openKey = NewKey();
        (HttpResponseMessage opened, JsonElement delivery) = await TwiceJsonAsync(failures, "open delivery", () => owner.PostJsonAsync("/staff/scan/api/deliveries", station, new { warehouseId = warehouse, carrier = "UPS", key = openKey }));
        Assert.True(opened.IsSuccessStatusCode, string.Join("\n", failures));
        Assert.True(opened.IsSuccessStatusCode, string.Join("\n", failures));
        Guid deliveryId = delivery.GetProperty("deliveryId").GetGuid();
        string scanKey = NewKey();
        (HttpResponseMessage _, JsonElement scanned) = await TwiceJsonAsync(failures, "scan, same key", () => ScanAsync(owner, station, deliveryId, "1Z999AA10123456784", scanKey));
        await TwiceJsonAsync(failures, "scan, two keys", () => ScanAsync(owner, station, deliveryId, "TBA123456789000"));
        Guid parcel = scanned.GetProperty("parcelId").GetGuid();
        (HttpResponseMessage _, string check) = await owner.GetAsync("/staff/inspection/" + parcel);
        await TwiceAsync(failures, "inspection", () => CheckAsync(owner, "/staff/inspection/" + parcel, check, "intact", new[] { "photo_package" }));
        (HttpResponseMessage _, string parcelPage) = await owner.GetAsync("/staff/parcels/" + parcel);
        string shelf = Regex.Match(parcelPage, "<option value=\"(?<id>[0-9a-f-]{36})\">B-01").Groups["id"].Value;
        string putKey = NewKey();
        await TwiceAsync(failures, "put away", () => owner.PostAsync("/staff/parcels/" + parcel + "/put-away", parcelPage, ("location", shelf), ("key", putKey)));
        await TwiceJsonAsync(failures, "close", () => owner.PostJsonAsync("/staff/scan/api/deliveries/" + deliveryId + "/close", station, new { }));
        (HttpResponseMessage _, string customer) = await owner.GetAsync("/staff/customers/" + ana);
        Assert.Contains(anaCode, customer, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    // A request that fails comes back to the error page with its own method; a failed form post used to meet a GET-only page and show a blank 404 or 405.
    [SqlServerFact]
    public async Task ErrorPage_AnswersEveryMethod_WithAWayBack()
    {
        NewTenant("errco", "Error Co");
        using HttpClient client = _host.Client("errco.techcrafters.ltd");

        foreach (HttpMethod method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            using HttpRequestMessage request = new(method, "/error");
            HttpResponseMessage response = await client.SendAsync(request);
            string html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("Something went wrong", html, StringComparison.Ordinal);
            Assert.Contains("href=\"/\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Vixly", Regex.Replace(html, "<[^>]+>", string.Empty), StringComparison.Ordinal);
        }
    }

    // Sends the same request twice at the same moment; anything but a page, a redirect or a refusal the page shows is a failure.
    private static async Task<(HttpResponseMessage Response, string Html)> TwiceAsync(List<string> failures, string name, Func<Task<(HttpResponseMessage Response, string Html)>> send)
    {
        (HttpResponseMessage Response, string Html)[] both = await Task.WhenAll(send(), send());

        foreach ((HttpResponseMessage response, string html) in both)
        {
            if ((int)response.StatusCode >= 400)
            {
                failures.Add(name + ": " + (int)response.StatusCode + " " + Regex.Match(html, "role=\"alert\"[^>]*>(?<m>[^<]*)").Groups["m"].Value + " " + (html.Length > 300 ? html[..300] : html));
            }
        }

        return both.FirstOrDefault(item => (int)item.Response.StatusCode < 400, both[0]);
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Json)> TwiceJsonAsync(List<string> failures, string name, Func<Task<(HttpResponseMessage Response, JsonElement Json)>> send)
    {
        (HttpResponseMessage Response, JsonElement Json)[] both = await Task.WhenAll(send(), send());

        foreach ((HttpResponseMessage response, JsonElement json) in both.Where(item => !item.Response.IsSuccessStatusCode))
        {
            failures.Add(name + ": " + (int)response.StatusCode + " " + (json.ValueKind == JsonValueKind.Object ? json.ToString() : string.Empty));
        }

        return both.FirstOrDefault(item => item.Response.IsSuccessStatusCode, both[0]);
    }

    private static string NewKey() => Guid.NewGuid().ToString("N");

    private static async Task<(string Station, Guid Delivery)> OpenAsync(CookieClient clerk, Guid warehouse, string carrier)
    {
        (HttpResponseMessage _, string station) = await clerk.GetAsync("/staff/scan?warehouse=" + warehouse);
        (HttpResponseMessage opened, JsonElement json) = await clerk.PostJsonAsync("/staff/scan/api/deliveries", station, new { warehouseId = warehouse, carrier, key = NewKey() });
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        return (station, json.GetProperty("deliveryId").GetGuid());
    }

    private static Task<(HttpResponseMessage Response, JsonElement Json)> ScanAsync(CookieClient clerk, string station, Guid delivery, string barcode, string? key = null,
        bool damaged = false, Guid? printer = null) =>
        clerk.PostJsonAsync("/staff/scan/api/deliveries/" + delivery + "/scans", station, new { barcode, key = key ?? NewKey(), damaged, printerId = printer });

    private static async Task<JsonElement> ScanOkAsync(CookieClient clerk, string station, Guid delivery, string barcode, string? key = null, bool damaged = false, Guid? printer = null)
    {
        (HttpResponseMessage response, JsonElement json) = await ScanAsync(clerk, station, delivery, barcode, key, damaged, printer);
        Assert.True(response.StatusCode == HttpStatusCode.OK, json.ValueKind == JsonValueKind.Object && json.TryGetProperty("message", out JsonElement message) ? message.GetString() : response.StatusCode.ToString());
        return json;
    }

    // The inspection form as the station sends it: condition, weight and size in the company's units, and the photos taken.
    private static Task<(HttpResponseMessage Response, string Html)> CheckAsync(CookieClient clerk, string path, string form, string condition, string[] photos) =>
        clerk.PostFilesAsync(path, form, photos.Select(field => (field, field + ".png", Png())).ToArray(),
            ("key", Regex.Match(form, "name=\"key\" value=\"(?<key>[0-9a-f]{32})\"").Groups["key"].Value), ("condition", condition),
            ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10.5"));

    private HttpClient Agent(string slug, string key)
    {
        HttpClient client = _host.Client(slug + ".techcrafters.ltd");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return client;
    }

    private static async Task<List<PrintJobDto>> JobsAsync(HttpClient program)
    {
        HttpResponseMessage response = await program.GetAsync("/agent/v1/print-jobs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<PrintJobDto>>())!;
    }

    // What the outbox worker does for the parcel's email.
    private async Task DispatchAsync(Guid parcel)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        OperationsOutboxMessage message = await context.OutboxMessages.SingleAsync(item => item.StreamId == parcel && item.Type == OperationsReceivingMessages.ParcelNotice);
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(message.TenantId!.Value);
        IOperationsOutboxHandler handler = scope.ServiceProvider.GetServices<IOperationsOutboxHandler>().Single(item => item.MessageType == OperationsReceivingMessages.ParcelNotice);
        await handler.HandleAsync(message, CancellationToken.None);
    }

    private async Task<Guid> WarehouseAsync(Guid tenant)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationsBranch branch = OperationsBranch.Create(tenant, Details("MIA", "Miami"), OperationsBranchSide.Origin, now);
        OperationsWarehouse warehouse = OperationsWarehouse.Create(tenant, branch.Id, Details("WH1", "Doral"), OperationsWarehouseRoles.Receiving, true, now);
        context.AddRange(branch, warehouse);
        await context.SaveChangesAsync();
        return warehouse.Id;
    }

    private static OperationsLocationDetails Details(string code, string name) =>
        new(code, name, OperationsPostalAddress.Create("8400 NW 25th St", null, "Doral", "FL", "33122", "US"), null, null, null, true);

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

    private async Task PreAlertAsync(Guid tenant, Guid customer, string tracking, string contents)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        // Announced the way the customer does it: inside a shipment that says where it goes (owner, 2026-10-18).
        Guid warehouse = (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Warehouses.FirstAsync()).Id;
        IOperationsReceivingRepository receiving = scope.ServiceProvider.GetRequiredService<IOperationsReceivingRepository>();
        // The customer's open shipment with instructions, or a new one: several packages are announced in one shipment.
        Vixly.Operations.Domain.Shipments.OperationsShipment? shipment = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Shipments
            .FirstOrDefaultAsync(item => item.CustomerId == customer && item.DestinationCountryCode != null && item.Status == Vixly.Operations.Domain.Shipments.OperationsShipmentStatus.Open);

        if (shipment is null)
        {
            shipment = Vixly.Operations.Domain.Shipments.OperationsShipment.Request(tenant, customer, warehouse,
                Vixly.Operations.Domain.Shipments.OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"),
                    "Ana Pierre", "+50937123456", Vixly.Operations.Domain.Shipments.OperationsShipmentOptions.None), DateTimeOffset.UtcNow);
            receiving.Add(shipment);
        }

        receiving.Add(OperationsPreAlert.Create(shipment, "UPS", tracking, "Shoe store", contents, 120m, "USD", null, DateTimeOffset.UtcNow));
        await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    // A PNG header: signature, then the IHDR chunk with the size.
    private static byte[] Png()
    {
        byte[] data = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(data, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(16), 640);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(20), 480);
        return data;
    }

    private static string Code(string manualKey, DateTime at) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(at);

    [GeneratedRegex("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant)]
    private static partial Regex ManualKey();

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

    // Ada, invited by the Owner with a role that edits the website only.
    private async Task<CookieClient> StaffAsync(CookieClient owner, string slug)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, ("name", "Website editor"), ("permissions", "ops.brand.view"), ("permissions", "ops.brand.edit"));
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Website editor</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", "Ada Lovelace"), ("email", "ada@" + slug + ".example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));

        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient ada = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await ada.GetAsync(new Uri(link).PathAndQuery);
        await ada.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(ada);
        return ada;
    }

    // Owner, 2026-10-20: the idle timer ends the session for real and lands on the plain sign-in page with the reason; a dead session says so too.
    [SqlServerFact]
    public async Task IdleSignOut_EndsTheSession_AndTheSignInPageSaysWhy()
    {
        NewTenant("idleco", "Idle Co");
        using CookieClient owner = await OwnerAsync("idleco");
        string token = owner.Cookie(OperationsCookies.Session)!;

        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        Assert.Contains("data-sign-out", home, StringComparison.Ordinal);
        (HttpResponseMessage idle, string _) = await owner.PostAsync("/staff/sign-out", home, ("reason", "idle"));
        Assert.Equal("/staff?idle=1", idle.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string signIn) = await owner.GetAsync("/staff?idle=1");
        Assert.Contains("You were signed out after 15 minutes without activity.", System.Net.WebUtility.HtmlDecode(signIn), StringComparison.Ordinal);
        Assert.Contains("name=\"password\"", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("ops-nav", signIn, StringComparison.Ordinal);

        // The old cookie sent again: the session is over, and the page says so.
        owner.Cookies.Add(new Uri("https://idleco.techcrafters.ltd"), new Cookie(OperationsCookies.Session, token, "/") { Secure = true, HttpOnly = true });
        (HttpResponseMessage ended, string _) = await owner.GetAsync("/staff/customers");
        Assert.Equal("/staff?ended=1", ended.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string again) = await owner.GetAsync("/staff?ended=1");
        Assert.Contains("Your session has ended.", System.Net.WebUtility.HtmlDecode(again), StringComparison.Ordinal);
    }

    private async Task<CookieClient> OwnerAsync(string slug) => (await OwnerWithKeyAsync(slug)).Browser;

    // Slice 5F step 2b: a person who enrols an authenticator app at the first sign-in, for the actions that ask for the second check.
    private async Task<CookieClient> PersonWithSecondFactorAsync(CookieClient owner, string slug, string roleName, string mailbox, params string[] permissions)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, new[] { ("name", roleName) }.Concat(permissions.Select(permission => ("permissions", permission))).ToArray());
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">" + Regex.Escape(roleName) + "</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", roleName + " Person"), ("email", mailbox + "@" + slug + ".example"), ("language", "en"), ("roleId", role),
            ("scope", "tenant"));
        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient person = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await person.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await person.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await person.GetAsync(accepted.Headers.Location!.OriginalString);
        await person.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(ManualKey().Match(enrol).Groups["key"].Value, DateTime.UtcNow)));
        await EnrolAsync(person);
        return person;
    }

    // Slice 5E: the owner with their authenticator secret, to approve a discount at the counter as the app would.
    private async Task<(CookieClient Browser, string ManualKey)> OwnerWithKeyAsync(string slug)
    {
        Guid tenant = _host.Platform.Portals[slug].TenantId!.Value;
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        string manualKey = ManualKey().Match(enrol).Groups["key"].Value;
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(manualKey, DateTime.UtcNow)));
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
        await EnrolAsync(browser);
        return (browser, manualKey);
    }

    // Slice 7: a dispatch to a warehouse that clears customs: the bundle, then its parcels; customs held then cleared; one parcel missing, lost, then found late;
    // ready for pickup once cleared, and one email when the whole shipment is ready.
    [SqlServerFact]
    public async Task Arrivals_ReceiveBundlesThenParcels_ClearCustoms_AndListWhatIsMissing()
    {
        Guid tenant = NewTenant("arriveco", "Arrive Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("arriveco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.arrive@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: true, withHub: false);
        (Guid shipment, List<OperationsParcel> parcels, string bundleNumber) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 2);
        string dispatchPath = await SentAsync(owner, origin, destination, bundleNumber);
        Guid dispatchId = Guid.Parse(dispatchPath[^36..]);
        string arrival = "/staff/arrivals/" + dispatchId;
        string here = "?warehouse=" + destination;

        // Coming: the dispatch is listed at its destination, and not at the origin.
        Assert.Contains(dispatchId.ToString(), (await owner.GetAsync("/staff/arrivals?warehouse=" + destination)).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(arrival + "?warehouse=" + origin)).Response.StatusCode);
        (HttpResponseMessage _, string page) = await owner.GetAsync(arrival + here);

        // Customs before anything was scanned in: nothing here yet (review).
        Assert.Contains("Not here", WebUtility.HtmlDecode((await owner.PostAsync(arrival + "/customs", page, ("warehouse", destination.ToString()), ("key", Key()), ("step", "in_customs"))).Html),
            StringComparison.Ordinal);

        // An unknown number is refused; the bundle arrives (the leg under way with it), twice is once; it cannot be finished before the bundle is opened.
        Assert.Contains("No bundle, box or parcel has that number.", WebUtility.HtmlDecode((await owner.PostAsync(arrival + "/scan", page, ("warehouse", destination.ToString()),
            ("code", "NOPE-1"))).Html), StringComparison.Ordinal);
        Assert.StartsWith(arrival + here + "&done=bundle&", (await owner.PostAsync(arrival + "/scan", page, ("warehouse", destination.ToString()), ("code", bundleNumber.ToLowerInvariant())))
            .Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.StartsWith(arrival + here + "&done=bundle_again&", (await owner.PostAsync(arrival + "/scan", page, ("warehouse", destination.ToString()), ("code", bundleNumber))).Response.Headers
            .Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(Vixly.Operations.Domain.Dispatch.OperationsDispatchStatus.Arrived, (await DispatchRowAsync(tenant, dispatchId)).Status);
        Assert.Equal(OperationsMilestone.ArrivedDestinationCountry, (await ParcelRowAsync(tenant, parcels[0].Id)).Milestone);
        Assert.Contains("Open each bundle that ends here", WebUtility.HtmlDecode((await owner.PostAsync(arrival + "/finish", page, ("warehouse", destination.ToString()))).Html),
            StringComparison.Ordinal);

        // Opened, the first parcel is scanned: it arrived, but waits for customs here.
        Guid bundleId = (await ParcelRowAsync(tenant, parcels[0].Id)).BundleId!.Value;
        Assert.Equal(arrival + here + "&done=opened", (await owner.PostAsync(arrival + "/bundles/" + bundleId + "/open", page, ("warehouse", destination.ToString()))).Response.Headers.Location
            ?.OriginalString);
        Assert.StartsWith(arrival + here + "&done=parcel&", (await owner.PostAsync(arrival + "/scan", page, ("warehouse", destination.ToString()), ("code", parcels[0].Number!))).Response
            .Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(OperationsMilestone.ArrivedDestinationWarehouse, (await ParcelRowAsync(tenant, parcels[0].Id)).Milestone);

        // Customs: held with a line for the customer (the parcels may not be collected), then cleared: the parcel here is ready for pickup.
        string heldKey = Key();
        Assert.Equal(arrival + here + "&done=customs", (await owner.PostAsync(arrival + "/customs", page, ("warehouse", destination.ToString()), ("key", heldKey), ("step", "held"),
            ("bundle", ""), ("message", "Invoice needed"))).Response.Headers.Location?.OriginalString);
        await owner.PostAsync(arrival + "/customs", page, ("warehouse", destination.ToString()), ("key", heldKey), ("step", "held"), ("bundle", ""), ("message", "Invoice needed"));
        // The bundle is open: only the parcel scanned in here is held, not the one missing from it (review).
        Assert.True((await ParcelRowAsync(tenant, parcels[0].Id)).Flags.HasFlag(OperationsParcelFlags.CustomsHeld));
        Assert.False((await ParcelRowAsync(tenant, parcels[1].Id)).Flags.HasFlag(OperationsParcelFlags.CustomsHeld));
        Assert.Contains("Choose a customs step.", WebUtility.HtmlDecode((await owner.PostAsync(arrival + "/customs", page, ("warehouse", destination.ToString()), ("key", Key()),
            ("step", "lost"))).Html), StringComparison.Ordinal);
        await owner.PostAsync(arrival + "/customs", page, ("warehouse", destination.ToString()), ("key", Key()), ("step", "cleared"), ("bundle", bundleId.ToString()));
        Assert.Equal(OperationsMilestone.ReadyForPickup, (await ParcelRowAsync(tenant, parcels[0].Id)).Milestone);
        Assert.False((await ParcelRowAsync(tenant, parcels[0].Id)).Flags.HasFlag(OperationsParcelFlags.CustomsHeld));
        // A notice is written; its handler sends nothing until the whole shipment is ready.
        Assert.Equal(1, await OutboxCountAsync(tenant, Vixly.Operations.Application.Arrivals.OperationsPickupNotice.MessageType));

        // Finished with the second parcel not scanned: it is missing; resolved as lost with a note, the customer has an exception and one email.
        Assert.Equal(arrival + here + "&done=finished_missing", (await owner.PostAsync(arrival + "/finish", page, ("warehouse", destination.ToString()))).Response.Headers.Location
            ?.OriginalString);
        Assert.Contains("This dispatch was received already.", WebUtility.HtmlDecode((await owner.PostAsync(arrival + "/finish", page, ("warehouse", destination.ToString()))).Html),
            StringComparison.Ordinal);
        OperationsArrivalDiscrepancy missing = Assert.Single(await DiscrepanciesAsync(tenant));
        Assert.Equal((OperationsDiscrepancyKind.MissingParcel, parcels[1].Id), (missing.Kind, missing.ParcelId!.Value));
        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/arrivals/discrepancies");
        Assert.Contains(parcels[1].Number!, list, StringComparison.Ordinal);
        Assert.Contains("Choose lost or settled and write a note.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/arrivals/discrepancies/" + missing.Id, list, ("outcome", "Lost"),
            ("note", " "))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/arrivals/discrepancies?done=1", (await owner.PostAsync("/staff/arrivals/discrepancies/" + missing.Id, list, ("outcome", "Lost"), ("note", "Not on the truck")))
            .Response.Headers.Location?.OriginalString);
        Assert.Contains("This discrepancy is closed already.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/arrivals/discrepancies/" + missing.Id, list, ("outcome", "Resolved"),
            ("note", "Again"))).Html), StringComparison.Ordinal);
        Assert.Equal(1, await OutboxCountAsync(tenant, "ops.events.update.v1"));

        // Found late: scanned after the dispatch was received, it arrives; cleared by customs, it is ready; the whole shipment is ready: one email.
        Assert.StartsWith(arrival + here + "&done=parcel&", (await owner.PostAsync(arrival + "/scan", page, ("warehouse", destination.ToString()), ("code", parcels[1].Number!))).Response
            .Headers.Location?.OriginalString, StringComparison.Ordinal);
        await owner.PostAsync(arrival + "/customs", page, ("warehouse", destination.ToString()), ("key", Key()), ("step", "cleared"), ("bundle", bundleId.ToString()));
        Assert.StartsWith(arrival + here + "&done=parcel_again&", (await owner.PostAsync(arrival + "/scan", page, ("warehouse", destination.ToString()), ("code", parcels[1].Number!))).Response
            .Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(OperationsMilestone.ReadyForPickup, (await ParcelRowAsync(tenant, parcels[1].Id)).Milestone);

        // Each request that made a parcel ready writes a notice; the email goes once, when the whole shipment is ready.
        Assert.Equal(2, await OutboxCountAsync(tenant, Vixly.Operations.Application.Arrivals.OperationsPickupNotice.MessageType));

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();

            foreach (OperationsOutboxMessage message in await context.OutboxMessages.Where(item => item.Type == Vixly.Operations.Application.Arrivals.OperationsPickupNotice.MessageType && item.TenantId == tenant).ToListAsync())
            {
                Assert.Equal(shipment, message.StreamId);
                await scope.ServiceProvider.GetServices<IOperationsOutboxHandler>().Single(item => item.MessageType == message.Type).HandleAsync(message, CancellationToken.None);
                await context.SaveChangesAsync();
            }

            Assert.NotNull((await context.Shipments.AsNoTracking().SingleAsync(item => item.Id == shipment)).PickupNotifiedUtc);
            List<OperationsTrackingEvent> events = await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectId == parcels[1].Id).ToListAsync();
            Assert.Equal("missing_in_transit", events.Single(item => item.Code == OperationsTrackingCodes.ExceptionRaised).Arguments()["cause"]);
            Assert.DoesNotContain(events, item => item.Code == OperationsTrackingCodes.CustomsHeld);
            Assert.Equal("Invoice needed", (await context.TrackingEvents.AsNoTracking().SingleAsync(item => item.SubjectId == parcels[0].Id
                && item.Code == OperationsTrackingCodes.CustomsHeld)).Arguments()["message"]);
            Assert.Single(events, item => item.Code == OperationsTrackingCodes.ArrivedDestinationWarehouse);
            Assert.Single(events, item => item.Code == OperationsTrackingCodes.ReadyForPickup);
            Assert.Equal(OperationsDiscrepancyStatus.Lost, (await context.ArrivalDiscrepancies.AsNoTracking().SingleAsync()).Status);
        }

        Vixly.Operations.Application.Ports.OperationsCustomerMail mail = Assert.Single(_host.CustomerMailer.Sent, item => item.Kind == OperationsCustomerMailKind.ReadyForPickup);
        Assert.Equal("Capois", mail.Extra["place"]);

        // Someone who only fills bundles sees no arrivals.
        using CookieClient worker = await PersonAsync(owner, "arriveco", "Bundler", "bundler.arrive", "ops.bundles.fill");
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.GetAsync("/staff/arrivals")).Html).ToLowerInvariant(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await worker.GetAsync(arrival + here)).Response.StatusCode);
    }

    // Slice 7 (R-11): a dispatch for a warehouse served by a hub is received at the hub; the bundle goes on unopened on an onward dispatch;
    // its parcel is in transit, never manifested twice, and is ready for pickup at its warehouse (no customs there).
    [SqlServerFact]
    public async Task Arrivals_AHubReceivesForTheWarehouseItServes_AndSendsTheBundleOn()
    {
        Guid tenant = NewTenant("hubco", "Hub Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("hubco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.hub@example.com");
        (Guid destination, Guid hub) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: true);
        (Guid _, List<OperationsParcel> parcels, string bundleNumber) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1);
        string first = await SentAsync(owner, origin, destination, bundleNumber);
        Guid firstId = Guid.Parse(first[^36..]);

        // Bound for the hub, not for the warehouse it serves.
        Assert.DoesNotContain(firstId.ToString(), (await owner.GetAsync("/staff/arrivals?warehouse=" + destination)).Html, StringComparison.Ordinal);
        string atHub = "/staff/arrivals/" + firstId;
        (HttpResponseMessage _, string hubPage) = await owner.GetAsync(atHub + "?warehouse=" + hub);
        Assert.StartsWith(atHub + "?warehouse=" + hub + "&done=bundle&", (await owner.PostAsync(atHub + "/scan", hubPage, ("warehouse", hub.ToString()), ("code", bundleNumber))).Response.Headers
            .Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(atHub + "?warehouse=" + hub + "&done=finished", (await owner.PostAsync(atHub + "/finish", hubPage, ("warehouse", hub.ToString()))).Response.Headers.Location?.OriginalString);
        OperationsParcel passing = await ParcelRowAsync(tenant, parcels[0].Id);
        Assert.Equal((hub, OperationsMilestone.ArrivedDestinationCountry), (passing.WarehouseId!.Value, passing.Milestone!.Value));

        // Onward from the hub: the bundle goes on; the parcel is in transit, not departed from its origin again.
        string onward = await SentAsync(owner, hub, destination, bundleNumber);
        Guid onwardId = Guid.Parse(onward[^36..]);
        Assert.True((await DispatchRowAsync(tenant, onwardId)).Onward);
        Assert.Contains(onwardId.ToString(), (await owner.GetAsync("/staff/arrivals?warehouse=" + destination)).Html, StringComparison.Ordinal);

        // At its warehouse, the parcel scanned straight away arrives with its bundle, which opens; no customs here: ready for pickup.
        string atEnd = "/staff/arrivals/" + onwardId;
        (HttpResponseMessage _, string endPage) = await owner.GetAsync(atEnd + "?warehouse=" + destination);
        Assert.StartsWith(atEnd + "?warehouse=" + destination + "&done=parcel_ready&", (await owner.PostAsync(atEnd + "/scan", endPage, ("warehouse", destination.ToString()),
            ("code", parcels[0].Number!))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(atEnd + "?warehouse=" + destination + "&done=finished", (await owner.PostAsync(atEnd + "/finish", endPage, ("warehouse", destination.ToString()))).Response.Headers.Location
            ?.OriginalString);

        // Scanned again at the hub on the first dispatch: nothing moves back (review).
        Assert.StartsWith(atHub + "?warehouse=" + hub + "&done=bundle_again&", (await owner.PostAsync(atHub + "/scan", hubPage, ("warehouse", hub.ToString()), ("code", bundleNumber))).Response
            .Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(destination, (await ParcelRowAsync(tenant, parcels[0].Id)).WarehouseId);
        Assert.Contains("Customs is not cleared at this warehouse.", WebUtility.HtmlDecode((await owner.PostAsync(atEnd + "/customs", endPage, ("warehouse", destination.ToString()),
            ("key", Key()), ("step", "held"))).Html), StringComparison.Ordinal);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        List<string> codes = await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectId == parcels[0].Id).Select(item => item.Code).ToListAsync();
        Assert.Single(codes, code => code == OperationsTrackingCodes.Manifested);
        Assert.Single(codes, code => code == OperationsTrackingCodes.DepartedOrigin);
        Assert.Single(codes, code => code == OperationsTrackingCodes.ArrivedDestinationCountry);
        Assert.Equal(2, codes.Count(code => code == OperationsTrackingCodes.InTransit));
        Assert.Single(codes, code => code == OperationsTrackingCodes.ReadyForPickup);
        Assert.Equal(OperationsMilestone.ReadyForPickup, (await context.Parcels.AsNoTracking().SingleAsync(item => item.Id == parcels[0].Id)).Milestone);
        Assert.Equal(2, await context.DispatchBundles.AsNoTracking().CountAsync());
        Assert.Empty(await context.ArrivalDiscrepancies.AsNoTracking().ToListAsync());
    }


    // Slice 7 step 2: the pickup counter: the company's proof and charges; storage billed with the destination charges; pay at the cashier, then release with ID, signature and code;
    // the person the customer named; a double click releases once; the customer sees it collected; someone without the right sees nothing.
    [SqlServerFact]
    public async Task Pickup_BillsChargesAndStorage_PaysFirst_ThenReleasesWithProof()
    {
        Guid tenant = NewTenant("pickupco", "Pickup Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("pickupco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.pickup@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: false);
        (Guid shipment, List<OperationsParcel> parcels, string bundleNumber) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1);

        // Settings → Pickup: storage needs a fee; then an ID, a signature and a code, destination charges, storage after 2 free days at 1.50.
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/pickup");
        Assert.Contains("a fee above zero when storage is charged", WebUtility.HtmlDecode((await owner.PostAsync("/staff/settings/pickup", settingsPage, ("proofs", "Id"), ("storage", "true"),
            ("freeDays", "2"), ("fee", ""))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/settings/pickup?done=1", (await owner.PostAsync("/staff/settings/pickup", settingsPage, ("proofs", "Id"), ("proofs", "Signature"), ("proofs", "Code"),
            ("charges", "true"), ("storage", "true"), ("freeDays", "2"), ("fee", "1.50"))).Response.Headers.Location?.OriginalString);

        // Received and ready at the destination; the customer named a cousin; ready for five days.
        string dispatchPath = await SentAsync(owner, origin, destination, bundleNumber);
        string arrival = "/staff/arrivals/" + dispatchPath[^36..];
        (HttpResponseMessage _, string arrivalPage) = await owner.GetAsync(arrival + "?warehouse=" + destination);
        Assert.Contains("done=parcel_ready", (await owner.PostAsync(arrival + "/scan", arrivalPage, ("warehouse", destination.ToString()), ("code", parcels[0].Number!))).Response.Headers.Location
            ?.OriginalString, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            OperationsShipment named = await context.Shipments.SingleAsync(item => item.Id == shipment);
            Assert.Equal(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.RecipientPhoneInvalid,
                Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(() => named.ChangeCollector("Marie Joseph", "37000000", DateTimeOffset.UtcNow)).Error);
            named.ChangeCollector("Marie Joseph", "+50937111111", DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ops].[Parcels] SET [ReadyUtc] = DATEADD(day, -5, [ReadyUtc]) WHERE [Id] = {parcels[0].Id}");
        }

        // The counter lists it, found by the cousin's phone; its page.
        Assert.Contains(shipment.ToString(), (await owner.GetAsync("/staff/pickup?warehouse=" + destination + "&query=7111111")).Html, StringComparison.Ordinal);
        Assert.DoesNotContain(shipment.ToString(), (await owner.GetAsync("/staff/pickup?warehouse=" + destination + "&query=nobody")).Html, StringComparison.Ordinal);
        string path = "/staff/pickup/" + shipment;
        string here = "?warehouse=" + destination;
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(path + "?warehouse=" + origin)).Response.StatusCode);
        (HttpResponseMessage _, string page) = await owner.GetAsync(path + here);
        Assert.Contains("Marie Joseph", page, StringComparison.Ordinal);
        Assert.Contains("Storage not billed yet: 4.50", WebUtility.HtmlDecode(page), StringComparison.Ordinal);
        (string, string)[] proof = { ("warehouse", destination.ToString()), ("collector", "Named"), ("idKind", "Passport"), ("idLast", "a123"),
            ("signature", "data:image/png;base64," + Convert.ToBase64String(Png())) };

        // Storage is billed first, with the duties typed; a bad amount is refused; twice is one invoice.
        Assert.Contains("Storage is owed: bill it first", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, proof.Append(("key", Key())).ToArray())).Html),
            StringComparison.Ordinal);
        Assert.Contains("Check the amounts", WebUtility.HtmlDecode((await owner.PostAsync(path + "/bill", page, ("warehouse", destination.ToString()), ("duties", "-1"))).Html),
            StringComparison.Ordinal);
        string billKey = Key();
        Assert.Equal(path + here + "&done=billed", (await owner.PostAsync(path + "/bill", page, ("warehouse", destination.ToString()), ("duties", "10"), ("localFee", "2.25"),
            ("key", billKey))).Response.Headers.Location?.OriginalString);
        await owner.PostAsync(path + "/bill", page, ("warehouse", destination.ToString()), ("duties", "10"), ("localFee", "2.25"), ("key", billKey));

        // Billed: no storage left to bill, so nothing new to bill without charges typed.
        Assert.Contains("There is nothing to bill.", WebUtility.HtmlDecode((await owner.PostAsync(path + "/bill", page, ("warehouse", destination.ToString()), ("key", Key()))).Html),
            StringComparison.Ordinal);
        Vixly.Operations.Domain.Billing.OperationsInvoice billed;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            billed = await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Invoices.AsNoTracking()
                .SingleAsync(item => item.ShipmentId == shipment && item.Kind == Vixly.Operations.Domain.Billing.OperationsInvoiceKind.Destination);
            Assert.Equal(new[] { Vixly.Operations.Domain.Charges.OperationsChargeLineKind.Duties, Vixly.Operations.Domain.Charges.OperationsChargeLineKind.LocalFee,
                Vixly.Operations.Domain.Charges.OperationsChargeLineKind.Storage }, billed.Lines.Select(line => line.ChargeKind!.Value).ToArray());
            Assert.Equal(4.50m, billed.Lines.Single(line => line.ChargeKind == Vixly.Operations.Domain.Charges.OperationsChargeLineKind.Storage).Gross);
            Assert.Equal(destination, billed.WarehouseId);
        }

        // Pay first: refused until the cashier at the destination takes the payment.
        (HttpResponseMessage _, string billedPage) = await owner.GetAsync(path + here);
        Assert.Contains("Everything is paid at the cashier first", WebUtility.HtmlDecode(billedPage), StringComparison.Ordinal);
        Assert.Contains("Every invoice of the shipment is paid first", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, proof.Append(("key", Key())).ToArray())).Html),
            StringComparison.Ordinal);
        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", destination.ToString()), ("float", "50"));
        (HttpResponseMessage _, string payPage) = await owner.GetAsync("/staff/cashier/invoices/" + billed.Id);
        await owner.PostAsync("/staff/cashier/invoices/" + billed.Id + "/pay", payPage, ("key", Key()), ("method_1", "Cash"),
            ("amount_1", billed.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));

        // The code: none sent yet, then one by email (its hash only is kept).
        Assert.Contains("No code was sent: send one to the customer first.", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, proof.Append(("key", Key())).ToArray())).Html),
            StringComparison.Ordinal);
        Assert.Equal(path + here + "&done=code", (await owner.PostAsync(path + "/code", page, ("warehouse", destination.ToString()))).Response.Headers.Location?.OriginalString);
        Vixly.Operations.Application.Ports.OperationsCustomerMail codeMail = _host.CustomerMailer.Sent.Last(item => item.Kind == OperationsCustomerMailKind.PickupCode);
        string code = codeMail.Extra["code"];
        Assert.Matches("^[0-9]{6}$", code);
        string wrong = code == "000000" ? "111111" : "000000";

        // The proof: the ID's last four characters, the signature and the code; each is checked.
        Assert.Contains("Choose the ID shown", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, ("warehouse", destination.ToString()), ("collector", "Named"),
            ("code", code), ("key", Key()))).Html), StringComparison.Ordinal);
        Assert.Contains("signs on the screen", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, ("warehouse", destination.ToString()), ("collector", "Named"),
            ("idKind", "Passport"), ("idLast", "A123"), ("code", code), ("key", Key()))).Html), StringComparison.Ordinal);
        Assert.Contains("does not match", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, proof.Append(("code", wrong)).Append(("key", Key())).ToArray())).Html),
            StringComparison.Ordinal);

        // Five wrong tries stop the code (review); a new one may be sent, not twice within two minutes.
        for (int tries = 2; tries < 5; tries++)
        {
            await owner.PostAsync(path + "/release", page, proof.Append(("code", wrong)).Append(("key", Key())).ToArray());
        }

        Assert.Contains("too many wrong tries", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, proof.Append(("code", wrong)).Append(("key", Key())).ToArray())).Html),
            StringComparison.Ordinal);
        Assert.Contains("too many wrong tries", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, proof.Append(("code", code)).Append(("key", Key())).ToArray())).Html),
            StringComparison.Ordinal);
        Assert.Contains("Wait two minutes", WebUtility.HtmlDecode((await owner.PostAsync(path + "/code", page, ("warehouse", destination.ToString()))).Html), StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Database
                .ExecuteSqlInterpolatedAsync($"UPDATE [ops].[Shipments] SET [PickupCodeUtc] = DATEADD(minute, -3, [PickupCodeUtc]) WHERE [Id] = {shipment}");
        }

        Assert.Equal(path + here + "&done=code", (await owner.PostAsync(path + "/code", page, ("warehouse", destination.ToString()))).Response.Headers.Location?.OriginalString);
        code = _host.CustomerMailer.Sent.Last(item => item.Kind == OperationsCustomerMailKind.PickupCode).Extra["code"];
        string releaseKey = Key();
        (string, string)[] good = proof.Append(("code", code)).Append(("key", releaseKey)).ToArray();
        Assert.Equal(path + here + "&done=released", (await owner.PostAsync(path + "/release", page, good)).Response.Headers.Location?.OriginalString);
        Assert.Equal(path + here + "&done=released", (await owner.PostAsync(path + "/release", page, good)).Response.Headers.Location?.OriginalString);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Pickup.OperationsPickupRelease release = await context.PickupReleases.AsNoTracking().SingleAsync();
            Assert.Equal(("Marie Joseph", Vixly.Operations.Domain.Pickup.OperationsPickupCollector.Named, "A123", true, 1),
                (release.CollectorName, release.Collector, release.IdLast, release.CodeChecked, release.ParcelCount));
            Assert.NotNull(release.SignatureKey);
            Assert.Equal(OperationsMilestone.Delivered, (await context.Parcels.AsNoTracking().SingleAsync(item => item.Id == parcels[0].Id)).Milestone);
            OperationsShipment collected = await context.Shipments.AsNoTracking().SingleAsync(item => item.Id == shipment);
            Assert.NotNull(collected.PickedUpUtc);
            Assert.DoesNotContain(code, collected.PickupCodeProtected ?? string.Empty, StringComparison.Ordinal);
            Assert.Single(await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectId == parcels[0].Id && item.Code == OperationsTrackingCodes.PickedUp).ToListAsync());
            Assert.Equal(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.CollectorLocked,
                Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(() => collected.ChangeCollector("Paul", "+50937222222", DateTimeOffset.UtcNow)).Error);
        }

        (HttpResponseMessage _, string done) = await owner.GetAsync(path + here);
        Assert.Contains("collected by Marie Joseph", WebUtility.HtmlDecode(done), StringComparison.Ordinal);
        Assert.DoesNotContain("/release", done, StringComparison.Ordinal);
        Assert.DoesNotContain(shipment.ToString(), (await owner.GetAsync("/staff/pickup?warehouse=" + destination)).Html, StringComparison.Ordinal);

        // Someone who only fills bundles sees no counter.
        using CookieClient worker = await PersonAsync(owner, "pickupco", "Bundler", "bundler.pickup", "ops.bundles.fill");
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.GetAsync("/staff/pickup")).Html).ToLowerInvariant(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await worker.GetAsync(path + here)).Response.StatusCode);
    }

    // Slice 7 step 4 (R-8): a partner agent, only while the plan includes partner agents; at the partner's warehouse only: receives, releases; no prices, invoices,
    // customers or other warehouses; no role can be added; no money where the partner does not handle it; refused at sign-in once the plan no longer has it.
    [SqlServerFact]
    public async Task PartnerAgent_WorksOnlyAtThePartnersWarehouse_SeesNoMoney_AndOnlyWhileThePlanHasIt()
    {
        Guid tenant = NewTenant("partnerco", "Partner Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("partnerco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.partner@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: false, partner: true);
        (Guid shipment, List<OperationsParcel> parcels, string bundleNumber) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1);

        // Not in the plan: refused. A warehouse the company runs itself: refused.
        _host.Features.Tenants[tenant] = new OperationsPlanFeatures(true, true) { PartnerAgents = false };
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        Assert.Contains("action=\"/staff/people/invite-partner\"", people, StringComparison.Ordinal);
        (string, string)[] invite = { ("fullName", "Paul Partner"), ("email", "paul@capois.example"), ("language", "en"), ("warehouseId", destination.ToString()) };
        Assert.Contains("Partner agents are not part of the company’s plan.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/people/invite-partner", people, invite)).Html),
            StringComparison.Ordinal);
        _host.Features.Tenants[tenant] = OperationsPlanFeatures.All;
        Assert.Contains("Choose a warehouse run by a partner.", WebUtility.HtmlDecode((await owner.PostAsync("/staff/people/invite-partner", people,
            invite.Take(3).Append(("warehouseId", origin.ToString())).ToArray())).Html), StringComparison.Ordinal);

        // Invited for the partner's warehouse; listed as its partner agent; no role can be added.
        string member = (await owner.PostAsync("/staff/people/invite-partner", people, invite)).Response.Headers.Location!.OriginalString;
        Assert.Contains("Partner agent · Capois", WebUtility.HtmlDecode((await owner.GetAsync("/staff/people")).Html), StringComparison.Ordinal);
        (HttpResponseMessage _, string memberPage) = await owner.GetAsync(member);
        Assert.DoesNotContain("/grant\"", memberPage, StringComparison.Ordinal);
        (HttpResponseMessage _, string roleForm) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", roleForm, ("name", "Clerk"), ("permissions", "ops.customers.view"));
        string clerk = Regex.Match((await owner.GetAsync("/staff/people")).Html, "<option value=\"(?<id>[0-9a-f-]{36})\">Clerk</option>").Groups["id"].Value;
        Assert.Contains("no other role", (await owner.PostAsync(member.Split('?')[0] + "/grant", memberPage, ("roleId", clerk), ("scope", "tenant"))).Html,
            StringComparison.Ordinal);

        string link = _host.Mailer.Invitations.Last().Link;
        using CookieClient paul = _host.Browser("partnerco.techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await paul.GetAsync(new Uri(link).PathAndQuery);
        await paul.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(paul);

        // The menu: arrivals, the counter and deliveries; nothing about customers, shipments, invoices or the cashier.
        (HttpResponseMessage _, string home) = await paul.GetAsync("/staff");
        Assert.Contains("href=\"/staff/pickup\"", home, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/customers\"", home, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/cashier\"", home, StringComparison.Ordinal);
        foreach (string refused in new[] { "/staff/customers", "/staff/shipments", "/staff/cashier", "/staff/people" })
        {
            Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await paul.GetAsync(refused)).Html).ToLowerInvariant(), StringComparison.Ordinal);
        }

        // Receives at the partner's warehouse, never at the origin.
        string dispatchPath = await SentAsync(owner, origin, destination, bundleNumber);
        string arrival = "/staff/arrivals/" + dispatchPath[^36..];
        (HttpResponseMessage _, string arrivalPage) = await paul.GetAsync(arrival + "?warehouse=" + destination);
        Assert.Contains("done=parcel_ready", (await paul.PostAsync(arrival + "/scan", arrivalPage, ("warehouse", destination.ToString()), ("code", parcels[0].Number!))).Response.Headers.Location
            ?.OriginalString, StringComparison.Ordinal);

        // The counter: no invoices or amounts; billing refused; the origin's counter not found.
        string path = "/staff/pickup/" + shipment;
        (HttpResponseMessage _, string page) = await paul.GetAsync(path + "?warehouse=" + destination);
        Assert.Contains("Nothing to pay here.", WebUtility.HtmlDecode(page), StringComparison.Ordinal);
        Assert.DoesNotContain("/staff/cashier/invoices/", page, StringComparison.Ordinal);
        Assert.DoesNotContain("/bill\"", page, StringComparison.Ordinal);
        Assert.Contains("Payments are not taken here", WebUtility.HtmlDecode((await paul.PostAsync(path + "/bill", page, ("warehouse", destination.ToString()), ("duties", "10"),
            ("key", Key()))).Html), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await paul.GetAsync(path + "?warehouse=" + origin)).Response.StatusCode);

        // Once the plan no longer has it: nothing is allowed, and the next sign-in is refused.
        _host.Features.Tenants[tenant] = new OperationsPlanFeatures(true, true) { PartnerAgents = false };
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await paul.GetAsync("/staff/pickup")).Html).ToLowerInvariant(), StringComparison.Ordinal);
        using CookieClient again = _host.Browser("partnerco.techcrafters.ltd");
        (HttpResponseMessage _, string signIn) = await again.GetAsync("/staff");
        Assert.Contains("Partner access is not available for this company right now.", WebUtility.HtmlDecode((await again.PostAsync("/staff/sign-in", signIn,
            ("email", "paul@capois.example"), ("password", Password))).Html), StringComparison.Ordinal);
        Assert.Null(again.Cookie(OperationsCookies.Session));
    }

    // Slice 7 step 3: home delivery: the company's settings; a route planned for a driver who signs in; started, the customers told; the driver records at the door with
    // the proof asked; a failure counts a try and the last one leaves the parcel waiting for pickup; the run sheet; the cash settled; no one else sees the route.
    [SqlServerFact]
    public async Task Delivery_PlansARoute_TheDriverRecordsAtTheDoor_TheLastFailureWaitsForPickup()
    {
        Guid tenant = NewTenant("deliveryco", "Delivery Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("deliveryco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.delivery@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: false, deliversHome: true);
        (Guid first, List<OperationsParcel> firstParcels, string firstBundle) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, home: true);
        (Guid second, List<OperationsParcel> secondParcels, string secondBundle) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, home: true);

        foreach ((string bundle, OperationsParcel parcel) in new[] { (firstBundle, firstParcels[0]), (secondBundle, secondParcels[0]) })
        {
            string dispatchPath = await SentAsync(owner, origin, destination, bundle);
            string arrival = "/staff/arrivals/" + dispatchPath[^36..];
            (HttpResponseMessage _, string arrivalPage) = await owner.GetAsync(arrival + "?warehouse=" + destination);
            await owner.PostAsync(arrival + "/scan", arrivalPage, ("warehouse", destination.ToString()), ("code", parcel.Number!));
            Assert.Equal(OperationsMilestone.ArrivedDestinationWarehouse, (await ParcelRowAsync(tenant, parcel.Id)).Milestone);
        }

        // Settings → Delivery: two tries, then it waits for pickup; a signature and the ID at the door.
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/delivery");
        Assert.Contains("tries from 1 to 10", WebUtility.HtmlDecode((await owner.PostAsync("/staff/settings/delivery", settingsPage, ("recording", "Phone"), ("tries", "0"),
            ("afterFailed", "WaitForPickup"))).Html), StringComparison.Ordinal);
        Assert.Equal("/staff/settings/delivery?done=1", (await owner.PostAsync("/staff/settings/delivery", settingsPage, ("recording", "Phone"), ("tries", "2"),
            ("afterFailed", "WaitForPickup"), ("proofs", "Signature"), ("proofs", "Id"), ("emails", "true"))).Response.Headers.Location?.OriginalString);

        // A driver who signs in; the desk lists both shipments and offers the driver.
        using CookieClient driver = await PersonAsync(owner, "deliveryco", "Driver", "driver.delivery", "ops.delivery.drive");
        string desk = "/staff/delivery?warehouse=" + destination;
        (HttpResponseMessage _, string deskPage) = await owner.GetAsync(desk);
        Assert.Contains(first.ToString(), deskPage, StringComparison.Ordinal);
        Assert.Contains(second.ToString(), deskPage, StringComparison.Ordinal);
        string driverId = Regex.Match(deskPage, "<option value=\"(?<id>[0-9a-f-]{36})\">Driver Person</option>").Groups["id"].Value;
        Assert.NotEmpty(driverId);
        string today = Regex.Match(deskPage, "name=\"date\" value=\"(?<day>[0-9-]{10})\"").Groups["day"].Value;
        Assert.Contains("Choose a driver who may drive", WebUtility.HtmlDecode((await owner.PostAsync("/staff/delivery", deskPage, ("warehouse", destination.ToString()), ("key", Key()),
            ("date", today), ("shipments", first.ToString()))).Html), StringComparison.Ordinal);
        string planKey = Key();
        (string, string)[] plan = { ("warehouse", destination.ToString()), ("key", planKey), ("date", today), ("driver", driverId), ("shipments", first.ToString()), ("shipments", second.ToString()) };
        string route = Regex.Match((await owner.PostAsync("/staff/delivery", deskPage, plan)).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/delivery/routes/[0-9a-f-]{36}").Value;
        Assert.NotEmpty(route);
        Assert.StartsWith(route, (await owner.PostAsync("/staff/delivery", deskPage, plan)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.DoesNotContain(first.ToString(), (await owner.GetAsync(desk)).Html, StringComparison.Ordinal);

        // The driver sees their route; someone without the right does not; the run sheet prints.
        Assert.Contains(route, (await driver.GetAsync("/staff/drive")).Html, StringComparison.Ordinal);
        using CookieClient worker = await PersonAsync(owner, "deliveryco", "Bundler", "bundler.delivery", "ops.bundles.fill");
        Assert.Equal(HttpStatusCode.NotFound, (await worker.GetAsync(route)).Response.StatusCode);
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await worker.GetAsync("/staff/delivery")).Html).ToLowerInvariant(), StringComparison.Ordinal);
        (HttpResponseMessage sheet, string _) = await owner.GetAsync(route + "/sheet");
        Assert.Equal("application/pdf", sheet.Content.Headers.ContentType?.MediaType);

        // The driver starts it: out for delivery, the customer told.
        (HttpResponseMessage _, string routePage) = await driver.GetAsync(route);
        Assert.Equal(route + "?done=started", (await driver.PostAsync(route + "/start", routePage)).Response.Headers.Location?.OriginalString);
        Assert.Equal(OperationsMilestone.OutForDelivery, (await ParcelRowAsync(tenant, firstParcels[0].Id)).Milestone);
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            IOperationsOutboxHandler handler = scope.ServiceProvider.GetServices<IOperationsOutboxHandler>()
                .Single(item => item.MessageType == Vixly.Operations.Application.Delivery.OperationsOutForDeliveryNotice.MessageType);

            foreach (OperationsOutboxMessage message in await context.OutboxMessages.AsNoTracking()
                .Where(item => item.TenantId == tenant && item.Type == Vixly.Operations.Application.Delivery.OperationsOutForDeliveryNotice.MessageType).ToListAsync())
            {
                await handler.HandleAsync(message, CancellationToken.None);
            }
        }

        Assert.Equal(2, _host.CustomerMailer.Sent.Count(item => item.Kind == OperationsCustomerMailKind.OutForDelivery && item.Extra["shipment"].Length > 0));

        // At the door: the proof asked is checked; delivered, then the other one failed.
        (HttpResponseMessage _, string outPage) = await driver.GetAsync(route);
        List<string> stops = Regex.Matches(outPage, route + "/stops/(?<id>[0-9a-f-]{36})\"").Select(match => match.Groups["id"].Value).Distinct().ToList();
        Assert.Equal(2, stops.Count);
        Assert.Contains("signs on the screen", WebUtility.HtmlDecode((await driver.PostAsync(route + "/stops/" + stops[0], outPage, ("outcome", "delivered"), ("receiver", "Jean Baptiste"),
            ("idKind", "Passport"), ("idLast", "a123"))).Html), StringComparison.Ordinal);
        Assert.Contains("name of the person who received it", WebUtility.HtmlDecode((await driver.PostAsync(route + "/stops/" + stops[0], outPage, ("outcome", "delivered"), ("receiver", " "),
            ("idKind", "Passport"), ("idLast", "a123"), ("signature", "data:image/png;base64," + Convert.ToBase64String(Png())))).Html), StringComparison.Ordinal);
        (string, string)[] delivered = { ("outcome", "delivered"), ("receiver", "Jean Baptiste"), ("idKind", "Passport"), ("idLast", "a123"),
            ("signature", "data:image/png;base64," + Convert.ToBase64String(Png())) };
        Assert.StartsWith(route + "?done=recorded", (await driver.PostAsync(route + "/stops/" + stops[0], outPage, delivered)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.StartsWith(route + "?done=recorded", (await driver.PostAsync(route + "/stops/" + stops[0], outPage, delivered)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.StartsWith(route + "?done=recorded", (await driver.PostAsync(route + "/stops/" + stops[1], outPage, ("outcome", "failed"), ("failure", "not_home"))).Response.Headers.Location
            ?.OriginalString, StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Assert.Equal(OperationsMilestone.Delivered, (await context.Parcels.AsNoTracking().SingleAsync(item => item.Id == firstParcels[0].Id)).Milestone);
            Assert.NotNull((await context.Shipments.AsNoTracking().SingleAsync(item => item.Id == first)).DeliveredUtc);
            OperationsParcel failed = await context.Parcels.AsNoTracking().SingleAsync(item => item.Id == secondParcels[0].Id);
            Assert.Equal((OperationsMilestone.ArrivedDestinationWarehouse, 1, (Guid?)null), (failed.Milestone, failed.DeliveryTries, failed.RouteId));
            Vixly.Operations.Domain.Delivery.OperationsDeliveryStop stop = await context.DeliveryStops.AsNoTracking().SingleAsync(item => item.ShipmentId == first);
            Assert.Equal(("Jean Baptiste", "A123", false), (stop.ReceiverName, stop.IdLast, stop.FromSheet));
            Assert.NotNull(stop.SignatureKey);
            Assert.Equal(Vixly.Operations.Domain.Delivery.OperationsRouteStatus.Returned, (await context.DeliveryRoutes.AsNoTracking().SingleAsync()).Status);
            Assert.Single(await context.TrackingEvents.AsNoTracking().Where(item => item.SubjectId == firstParcels[0].Id && item.Code == OperationsTrackingCodes.Delivered).ToListAsync());
        }

        // Back: the driver cannot settle; the planner does, with nothing collected.
        (HttpResponseMessage _, string backPage) = await owner.GetAsync(route);
        (HttpResponseMessage _, string driverBack) = await driver.GetAsync(route);
        Assert.DoesNotContain("/settle", driverBack, StringComparison.Ordinal);
        Assert.Contains("your role does not allow this", WebUtility.HtmlDecode((await driver.PostAsync(route + "/settle", driverBack, ("handedIn", "0"))).Html).ToLowerInvariant(),
            StringComparison.Ordinal);
        Assert.Equal(route + "?done=settled", (await owner.PostAsync(route + "/settle", backPage, ("handedIn", "0"))).Response.Headers.Location?.OriginalString);

        // A second try fails too: the last one; it now waits for pickup at the counter.
        (HttpResponseMessage _, string againPage) = await owner.GetAsync(desk);
        Assert.Contains(second.ToString(), againPage, StringComparison.Ordinal);
        string again = Regex.Match((await owner.PostAsync("/staff/delivery", againPage, ("warehouse", destination.ToString()), ("key", Key()), ("date", today), ("driver", driverId),
            ("shipments", second.ToString()))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/delivery/routes/[0-9a-f-]{36}").Value;
        (HttpResponseMessage _, string againRoute) = await owner.GetAsync(again);
        Assert.Equal(again + "?done=started", (await owner.PostAsync(again + "/start", againRoute)).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string againOut) = await driver.GetAsync(again);
        string lastStop = Regex.Match(againOut, again + "/stops/(?<id>[0-9a-f-]{36})\"").Groups["id"].Value;
        Assert.StartsWith(again + "?done=recorded", (await driver.PostAsync(again + "/stops/" + lastStop, againOut, ("outcome", "failed"), ("failure", "other"), ("note", "Gate locked")))
            .Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        OperationsParcel waiting = await ParcelRowAsync(tenant, secondParcels[0].Id);
        Assert.Equal((OperationsMilestone.ReadyForPickup, 2), (waiting.Milestone, waiting.DeliveryTries));
        Assert.Contains(second.ToString(), (await owner.GetAsync("/staff/pickup?warehouse=" + destination)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain(second.ToString(), (await owner.GetAsync(desk)).Html, StringComparison.Ordinal);
    }

    // Slice 7 step 3: the run sheet and cash on delivery: duties billed before the round; a driver named only; staff enter the sheet (the signature on paper); exactly what is owed
    // is collected; the last failure sends the parcel back; the route settles once the cash is paid in, a difference with a note; the parcel leaves for its sender.
    [SqlServerFact]
    public async Task Delivery_FromTheRunSheet_CollectsCash_SettlesOncePaidIn_AndReturnsTheLastFailure()
    {
        Guid tenant = NewTenant("routeco", "Route Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("routeco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.route@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: false, deliversHome: true);
        (Guid paid, List<OperationsParcel> paidParcels, string paidBundle) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, home: true);
        (Guid refused, List<OperationsParcel> refusedParcels, string refusedBundle) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, home: true);

        foreach ((string bundle, OperationsParcel parcel) in new[] { (paidBundle, paidParcels[0]), (refusedBundle, refusedParcels[0]) })
        {
            string dispatchPath = await SentAsync(owner, origin, destination, bundle);
            string arrival = "/staff/arrivals/" + dispatchPath[^36..];
            (HttpResponseMessage _, string arrivalPage) = await owner.GetAsync(arrival + "?warehouse=" + destination);
            await owner.PostAsync(arrival + "/scan", arrivalPage, ("warehouse", destination.ToString()), ("code", parcel.Number!));
        }

        (HttpResponseMessage _, string pickupSettings) = await owner.GetAsync("/staff/settings/pickup");
        await owner.PostAsync("/staff/settings/pickup", pickupSettings, ("proofs", "Id"), ("charges", "true"), ("freeDays", "7"));
        (HttpResponseMessage _, string settingsPage) = await owner.GetAsync("/staff/settings/delivery");
        Assert.Equal("/staff/settings/delivery?done=1", (await owner.PostAsync("/staff/settings/delivery", settingsPage, ("recording", "Sheet"), ("cash", "true"), ("tries", "1"),
            ("afterFailed", "ReturnToOrigin"), ("proofs", "Signature"), ("proofs", "Id"))).Response.Headers.Location?.OriginalString);

        // Duties billed at the delivery desk: collected at the door.
        string desk = "/staff/delivery?warehouse=" + destination;
        (HttpResponseMessage _, string deskPage) = await owner.GetAsync(desk);
        Assert.Equal(desk + "&done=billed", (await owner.PostAsync("/staff/delivery/bill", deskPage, ("warehouse", destination.ToString()), ("shipment", paid.ToString()), ("duties", "10"),
            ("key", Key()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string billedDesk) = await owner.GetAsync(desk);
        Assert.Contains("Collect 10.00", WebUtility.HtmlDecode(billedDesk), StringComparison.Ordinal);
        string today = Regex.Match(billedDesk, "name=\"date\" value=\"(?<day>[0-9-]{10})\"").Groups["day"].Value;
        string route = Regex.Match((await owner.PostAsync("/staff/delivery", billedDesk, ("warehouse", destination.ToString()), ("key", Key()), ("date", today), ("driverName", "Paul Hired"),
            ("shipments", paid.ToString()), ("shipments", refused.ToString()))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/delivery/routes/[0-9a-f-]{36}").Value;
        Assert.NotEmpty(route);
        (HttpResponseMessage _, string planned) = await owner.GetAsync(route);
        Assert.Equal(route + "?done=started", (await owner.PostAsync(route + "/start", planned)).Response.Headers.Location?.OriginalString);

        // From the sheet: no signature asked; exactly what is owed.
        (HttpResponseMessage _, string outPage) = await owner.GetAsync(route);
        Assert.DoesNotContain("data-signature-pad=\"on\"", outPage, StringComparison.Ordinal);
        List<string> stops = Regex.Matches(outPage, route + "/stops/(?<id>[0-9a-f-]{36})\"").Select(match => match.Groups["id"].Value).Distinct().ToList();
        (string, string)[] door = { ("outcome", "delivered"), ("receiver", "Jean Baptiste"), ("idKind", "NationalId"), ("idLast", "9z9z") };
        Assert.Contains("exactly what is owed", WebUtility.HtmlDecode((await owner.PostAsync(route + "/stops/" + stops[0], outPage, door.Append(("cash", "5")).ToArray())).Html),
            StringComparison.Ordinal);
        Assert.StartsWith(route + "?done=recorded", (await owner.PostAsync(route + "/stops/" + stops[0], outPage, door.Append(("cash", "10.00")).ToArray())).Response.Headers.Location
            ?.OriginalString, StringComparison.Ordinal);
        Assert.StartsWith(route + "?done=recorded", (await owner.PostAsync(route + "/stops/" + stops[1], outPage, ("outcome", "failed"), ("failure", "refused"))).Response.Headers.Location
            ?.OriginalString, StringComparison.Ordinal);

        // The last try: it goes back; listed to return.
        OperationsParcel back = await ParcelRowAsync(tenant, refusedParcels[0].Id);
        Assert.True(back.Flags.HasFlag(OperationsParcelFlags.ReturnPending));
        (HttpResponseMessage _, string returnsPage) = await owner.GetAsync(desk);
        Assert.Contains("/staff/delivery/returns/" + refusedParcels[0].Id, returnsPage, StringComparison.Ordinal);

        // Settled once the cash is paid in at the cashier; a difference needs a note.
        (HttpResponseMessage _, string backPage) = await owner.GetAsync(route);
        Assert.Contains("still unpaid", WebUtility.HtmlDecode((await owner.PostAsync(route + "/settle", backPage, ("handedIn", "10.00"))).Html), StringComparison.Ordinal);
        Vixly.Operations.Domain.Billing.OperationsInvoice duties;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            duties = await context.Invoices.AsNoTracking().SingleAsync(item => item.ShipmentId == paid && item.Kind == Vixly.Operations.Domain.Billing.OperationsInvoiceKind.Destination);
            Vixly.Operations.Domain.Delivery.OperationsDeliveryStop stop = await context.DeliveryStops.AsNoTracking().SingleAsync(item => item.ShipmentId == paid);
            Assert.Equal((10m, true, (string?)null, "9Z9Z"), (stop.CashCollected, stop.FromSheet, stop.SignatureKey, stop.IdLast));
        }

        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", destination.ToString()), ("float", "20"));
        (HttpResponseMessage _, string payPage) = await owner.GetAsync("/staff/cashier/invoices/" + duties.Id);
        await owner.PostAsync("/staff/cashier/invoices/" + duties.Id + "/pay", payPage, ("key", Key()), ("method_1", "Cash"), ("amount_1", "10.00"));
        (HttpResponseMessage _, string paidIn) = await owner.GetAsync(route);
        Assert.Contains("exactly what is owed", WebUtility.HtmlDecode((await owner.PostAsync(route + "/settle", paidIn, ("handedIn", "9.00"))).Html), StringComparison.Ordinal);
        Assert.Equal(route + "?done=settled", (await owner.PostAsync(route + "/settle", paidIn, ("handedIn", "9.00"), ("note", "1.00 short"))).Response.Headers.Location?.OriginalString);

        // The parcel leaves for its sender.
        Assert.Equal(desk + "&done=returned", (await owner.PostAsync("/staff/delivery/returns/" + refusedParcels[0].Id, returnsPage, ("warehouse", destination.ToString()),
            ("note", "Back on the next truck"))).Response.Headers.Location?.OriginalString);
        OperationsParcel returned = await ParcelRowAsync(tenant, refusedParcels[0].Id);
        Assert.Equal(OperationsMilestone.ReturnedToSender, returned.Milestone);
        Assert.False(returned.Flags.HasFlag(OperationsParcelFlags.ReturnPending));
        Assert.Equal(OperationsMilestone.Delivered, (await ParcelRowAsync(tenant, paidParcels[0].Id)).Milestone);
    }

    // Freight collect (owner, 2026-10-21): offered only when the company allows it; the shipment moves unpaid, its invoice says so; the counter at the destination hands it over
    // only once paid there. A prepaid shipment still waits at the origin until paid.
    [SqlServerFact]
    public async Task FreightCollect_MovesUnpaid_AndIsHandedOverOncePaidAtTheDestination()
    {
        Guid tenant = NewTenant("collectco", "Collect Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("collectco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.collect@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: false);

        // Not offered: refused.
        string customerPath = "/staff/counter/customers/" + ana;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        Assert.DoesNotContain("name=\"collect\"", customerPage, StringComparison.Ordinal);
        Assert.Contains("Paying at the destination is not offered", WebUtility.HtmlDecode((await owner.PostAsync(customerPath + "/shipments", customerPage,
            ("key", FormKey(customerPage, customerPath + "/shipments")), ("origin", origin.ToString()), ("destination", destination.ToString()), ("delivery", "pickup"), ("collect", "true"),
            ("service", "air_standard"), ("parcelCount", "1"), ("recipient", "Jean Baptiste"), ("recipientPhone", "+50937000000"))).Html), StringComparison.Ordinal);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await context.TenantSettings.SingleOrDefaultAsync()
                ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(tenant, DateTimeOffset.UtcNow);
            settings.ChangeWork(settings.WeightUnit, settings.DimensionUnit, true, settings.AllowCredit, settings.DeliveryModes, DateTimeOffset.UtcNow);

            if (context.Entry(settings).State == EntityState.Detached)
            {
                context.Add(settings);
            }

            await context.SaveChangesAsync();
        }

        Assert.Contains("name=\"collect\"", (await owner.GetAsync(customerPath)).Html, StringComparison.Ordinal);
        (Guid shipment, List<OperationsParcel> parcels, string bundle) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, collect: true);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = await context.Invoices.AsNoTracking().SingleAsync(item => item.ShipmentId == shipment);
            Assert.True(invoice.PayAtDestination);
            Assert.Equal(Vixly.Operations.Domain.Billing.OperationsPaymentState.Unpaid, invoice.PaymentState);
            Assert.True((await context.Shipments.AsNoTracking().SingleAsync(item => item.Id == shipment)).FreightCollect);
        }

        // A prepaid shipment not paid stays at the origin.
        (Guid _, List<OperationsParcel> prepaidParcels, string _) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, pay: false);
        Assert.Null((await ParcelRowAsync(tenant, prepaidParcels[0].Id)).BundleId);
        Assert.NotNull((await ParcelRowAsync(tenant, parcels[0].Id)).BundleId);

        // Sent and ready at the destination: the counter hands it over only once paid there.
        string dispatchPath = await SentAsync(owner, origin, destination, bundle);
        string arrival = "/staff/arrivals/" + dispatchPath[^36..];
        (HttpResponseMessage _, string arrivalPage) = await owner.GetAsync(arrival + "?warehouse=" + destination);
        await owner.PostAsync(arrival + "/scan", arrivalPage, ("warehouse", destination.ToString()), ("code", parcels[0].Number!));
        Assert.Equal(OperationsMilestone.ReadyForPickup, (await ParcelRowAsync(tenant, parcels[0].Id)).Milestone);
        string path = "/staff/pickup/" + shipment;
        (HttpResponseMessage _, string page) = await owner.GetAsync(path + "?warehouse=" + destination);
        Assert.Contains("Every invoice of the shipment is paid first", WebUtility.HtmlDecode((await owner.PostAsync(path + "/release", page, ("warehouse", destination.ToString()),
            ("collector", "Recipient"), ("idKind", "Passport"), ("idLast", "A123"), ("signature", "data:image/png;base64," + Convert.ToBase64String(Png())), ("key", Key()))).Html),
            StringComparison.Ordinal);
    }

    // The bundle's list (owner, 2026-10-21): built from the computer, confirmed by scans; entered, scanned and missing; a parcel off the list refused; no closing while
    // something is missing; what is on a list is not ready for another bundle; scan-only bundles keep working.
    [SqlServerFact]
    public async Task BundleList_BuiltAtTheDesk_ConfirmedByScans_ClosesOnlyWhenNothingIsMissing()
    {
        Guid tenant = NewTenant("listco", "List Co");
        (CookieClient owner, string _) = await OwnerWithKeyAsync("listco");
        using CookieClient _owner = owner;
        Guid origin = await WarehouseAsync(tenant);
        (Guid ana, string _) = await CustomerAsync(tenant, "ana.list@example.com");
        (Guid destination, Guid _) = await DestinationsAsync(tenant, clearsCustoms: false, withHub: false);
        (Guid _, List<OperationsParcel> listed, string _) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 2, bundle: false);
        (Guid _, List<OperationsParcel> other, string _) = await BundledShipmentAsync(tenant, owner, ana, origin, destination, 1, bundle: false);

        (HttpResponseMessage _, string deskPage) = await owner.GetAsync("/staff/bundles");
        string bundlePath = Regex.Match((await owner.PostAsync("/staff/bundles", deskPage, ("key", Key()), ("warehouse", origin.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"), ("basis", "Free"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/bundles/[0-9a-f-]{36}").Value;
        (HttpResponseMessage _, string page) = await owner.GetAsync(bundlePath);
        Assert.Contains(listed[0].Id.ToString(), page, StringComparison.Ordinal);
        Assert.Contains("Choose at least one", WebUtility.HtmlDecode((await owner.PostAsync(bundlePath + "/list", page)).Html), StringComparison.Ordinal);
        Assert.Equal(bundlePath + "?done=listed", (await owner.PostAsync(bundlePath + "/list", page, ("items", listed[0].Id.ToString()), ("items", listed[1].Id.ToString())))
            .Response.Headers.Location?.OriginalString);
        string html = WebUtility.HtmlDecode((await owner.GetAsync(bundlePath)).Html);
        Assert.Contains("Entered: 2", html, StringComparison.Ordinal);
        Assert.Contains("Missing: 2", html, StringComparison.Ordinal);

        // At the station: off the list refused; on it confirmed; closing waits for the last one.
        (HttpResponseMessage _, string fill) = await owner.GetAsync(bundlePath + "/fill");
        Assert.Contains("not on it", WebUtility.HtmlDecode((await owner.PostAsync(bundlePath + "/scan", fill, ("key", Key()), ("code", other[0].Number!))).Html), StringComparison.Ordinal);
        await owner.PostAsync(bundlePath + "/scan", fill, ("key", Key()), ("code", listed[0].Number!));
        (HttpResponseMessage _, string half) = await owner.GetAsync(bundlePath);
        Assert.Contains("Scanned: 1", WebUtility.HtmlDecode(half), StringComparison.Ordinal);
        Assert.Contains("scan it, or take it off the list", WebUtility.HtmlDecode((await owner.PostAsync(bundlePath + "/close", half, ("weight", "9"), ("length", "40"), ("width", "30"),
            ("height", "20"))).Html), StringComparison.Ordinal);

        // Another bundle: a listed parcel is not ready for it and goes nowhere else; an unlisted one still goes in by scan alone.
        (HttpResponseMessage _, string desk2) = await owner.GetAsync("/staff/bundles");
        string second = Regex.Match((await owner.PostAsync("/staff/bundles", desk2, ("key", Key()), ("warehouse", origin.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"), ("basis", "Free"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/bundles/[0-9a-f-]{36}").Value;
        (HttpResponseMessage _, string secondPage) = await owner.GetAsync(second);
        Assert.DoesNotContain(listed[1].Id.ToString(), secondPage, StringComparison.Ordinal);
        (HttpResponseMessage _, string fill2) = await owner.GetAsync(second + "/fill");
        Assert.Contains("another bundle", WebUtility.HtmlDecode((await owner.PostAsync(second + "/scan", fill2, ("key", Key()), ("code", listed[1].Number!))).Html), StringComparison.Ordinal);
        await owner.PostAsync(second + "/scan", fill2, ("key", Key()), ("code", other[0].Number!));
        Assert.NotNull((await ParcelRowAsync(tenant, other[0].Id)).BundleId);

        // The last one scanned: closed.
        await owner.PostAsync(bundlePath + "/scan", fill, ("key", Key()), ("code", listed[1].Number!));
        (HttpResponseMessage _, string full) = await owner.GetAsync(bundlePath);
        Assert.Contains("Missing: 0", WebUtility.HtmlDecode(full), StringComparison.Ordinal);
        Assert.Equal(bundlePath + "?done=closed", (await owner.PostAsync(bundlePath + "/close", full, ("weight", "9"), ("length", "40"), ("width", "30"), ("height", "20")))
            .Response.Headers.Location?.OriginalString);
    }

    // A destination in Haiti that clears customs or not, and with a hub that serves it when asked.
    private async Task<(Guid Destination, Guid Hub)> DestinationsAsync(Guid tenant, bool clearsCustoms, bool withHub, bool deliversHome = false, bool partner = false)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-1);
        OperationsBranch haiti = OperationsBranch.Create(tenant, new OperationsLocationDetails("PAP", "Port-au-Prince",
            OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsBranchSide.Destination, now);
        OperationsWarehouse pickup = OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("PAP1", "Capois",
            OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, now);
        OperationsWarehouse? hub = withHub
            ? OperationsWarehouse.Create(tenant, haiti.Id, new OperationsLocationDetails("HUB", "Airport hub",
                OperationsPostalAddress.Create("1 Route de l'Aeroport", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.DispatchHub, false, now)
            : null;
        pickup.ChangeDestination(partner
            ? new OperationsDestinationTerms(deliversHome, hub?.Id, OperationsDestinationOperator.Partner, "Capois Partners", null, null, null) { ClearsCustoms = clearsCustoms }
            : new OperationsDestinationTerms(deliversHome, hub?.Id, OperationsDestinationOperator.Company, null, null, null, null) { ClearsCustoms = clearsCustoms }, now);

        // Slice 7 step 3: the company delivers to homes too.
        if (deliversHome)
        {
            Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await context.TenantSettings.SingleOrDefaultAsync()
                ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(tenant, now);
            settings.ChangeWork(settings.WeightUnit, settings.DimensionUnit, settings.AllowCollect, settings.AllowCredit,
                Vixly.Operations.Domain.Settings.OperationsDeliveryModes.Pickup | Vixly.Operations.Domain.Settings.OperationsDeliveryModes.HomeDelivery, now);

            if (context.Entry(settings).State == EntityState.Detached)
            {
                context.Add(settings);
            }
        }
        Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup air = Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup.Create(tenant, "air_standard", now);
        air.Change(true, null, 1, 5, 5000m, true, now);
        context.AddRange(haiti, pickup, air);

        if (hub is not null)
        {
            context.Add(hub);
        }

        context.Add(Vixly.Operations.Domain.Charges.OperationsRateCard.Create(tenant, "air_standard", null, null, null, Vixly.Operations.Domain.Charges.OperationsCustomsPolicy.TransportOnly,
            new Vixly.Operations.Domain.Charges.OperationsRatePrices(new[] { new Vixly.Operations.Domain.Charges.OperationsRateBand(0, Vixly.Operations.Domain.Charges.OperationsBandKind.PerUnit, 3m) },
                Array.Empty<Vixly.Operations.Domain.Charges.OperationsRateItem>(), null, null, null), null, now));
        await context.SaveChangesAsync();
        return (pickup.Id, hub?.Id ?? Guid.Empty);
    }

    // A paid pickup shipment with its parcels in one closed bundle for the destination.
    private async Task<(Guid Shipment, List<OperationsParcel> Parcels, string BundleNumber)> BundledShipmentAsync(Guid tenant, CookieClient owner, Guid customer, Guid origin, Guid destination,
        int count, bool home = false, bool collect = false, bool pay = true, bool bundle = true)
    {
        string customerPath = "/staff/counter/customers/" + customer;
        (HttpResponseMessage _, string customerPage) = await owner.GetAsync(customerPath);
        (string, string)[] where = home
            ? new[] { ("delivery", "home"), ("line1", "5 Rue Pavée"), ("city", "Port-au-Prince"), ("country", "HT") }
            : new[] { ("delivery", "pickup") };
        where = collect ? where.Append(("collect", "true")).ToArray() : where;
        (HttpResponseMessage created, string _) = await owner.PostAsync(customerPath + "/shipments", customerPage, new[] { ("key", FormKey(customerPage, customerPath + "/shipments")),
            ("origin", origin.ToString()), ("destination", destination.ToString()) }.Concat(where).Concat(new[] { ("service", "air_standard"),
            ("parcelCount", count.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("recipient", "Jean Baptiste"), ("recipientPhone", "+50937000000") }).ToArray());
        string shipmentPath = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, "^(?<path>/staff/counter/shipments/[0-9a-f-]{36})").Groups["path"].Value;

        for (int index = 0; index < count; index++)
        {
            (HttpResponseMessage _, string shipmentPage) = await owner.GetAsync(shipmentPath);
            await owner.PostAsync(shipmentPath + "/parcels", shipmentPage, ("key", FormKey(shipmentPage, shipmentPath + "/parcels")), ("weight", "4"), ("length", "30"), ("width", "20"),
                ("height", "10"), ("contents", "Shoes"), ("value", "60"));
        }

        Guid shipment = Guid.Parse(shipmentPath[^36..]);
        (HttpResponseMessage _, string drawers) = await owner.GetAsync("/staff/cashier/drawers");
        await owner.PostAsync("/staff/cashier/drawers/open", drawers, ("warehouse", origin.ToString()), ("float", "100"));
        List<OperationsParcel> parcels;

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            Vixly.Operations.Domain.Billing.OperationsInvoice invoice = await context.Invoices.AsNoTracking().SingleAsync(item => item.ShipmentId == shipment);

            // Freight collect travels unpaid (owner, 2026-10-21); an unpaid prepaid one waits at the origin.
            if (pay && !collect)
            {
                (HttpResponseMessage _, string payPage) = await owner.GetAsync("/staff/cashier/invoices/" + invoice.Id);
                await owner.PostAsync("/staff/cashier/invoices/" + invoice.Id + "/pay", payPage, ("key", Key()), ("method_1", "Cash"),
                    ("amount_1", invoice.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            }

            parcels = await context.Parcels.AsNoTracking().Where(item => item.ShipmentId == shipment).OrderBy(item => item.Number).ToListAsync();
        }

        Assert.Equal(count, parcels.Count);

        if (!bundle)
        {
            return (shipment, parcels, string.Empty);
        }

        (HttpResponseMessage _, string deskPage) = await owner.GetAsync("/staff/bundles");
        string bundlePath = Regex.Match((await owner.PostAsync("/staff/bundles", deskPage, ("key", Key()), ("warehouse", origin.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"), ("basis", "Free"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/bundles/[0-9a-f-]{36}").Value;
        string bundleNumber = Regex.Match((await owner.GetAsync(bundlePath)).Html, "Bundle (?<number>BD[0-9]+)").Groups["number"].Value;
        (HttpResponseMessage _, string fill) = await owner.GetAsync(bundlePath + "/fill");

        foreach (OperationsParcel parcel in parcels)
        {
            await owner.PostAsync(bundlePath + "/scan", fill, ("key", Key()), ("code", parcel.Number!));
        }

        (HttpResponseMessage _, string closePage) = await owner.GetAsync(bundlePath);
        await owner.PostAsync(bundlePath + "/close", closePage, ("weight", "9"), ("length", "40"), ("width", "30"), ("height", "20"));
        return (shipment, parcels, bundleNumber);
    }

    // A dispatch from a warehouse to the destination with the bundle and one truck leg, closed and departed.
    private static async Task<string> SentAsync(CookieClient owner, Guid from, Guid destination, string bundleNumber)
    {
        (HttpResponseMessage _, string desk) = await owner.GetAsync("/staff/dispatches");
        string dispatchPath = Regex.Match((await owner.PostAsync("/staff/dispatches", desk, ("key", Key()), ("warehouse", from.ToString()), ("destination", "w:" + destination.ToString("N")),
            ("service", "air_standard"))).Response.Headers.Location?.OriginalString ?? string.Empty, "^/staff/dispatches/[0-9a-f-]{36}").Value;
        Assert.NotEmpty(dispatchPath);
        (HttpResponseMessage _, string page) = await owner.GetAsync(dispatchPath);
        Assert.Equal(dispatchPath + "?done=bundle", (await owner.PostAsync(dispatchPath + "/bundles", page, ("number", bundleNumber))).Response.Headers.Location?.OriginalString);
        await owner.PostAsync(dispatchPath + "/legs", page, ("key", Key()), ("kind", "Ground"), ("from", "MIA"), ("to", "PAP"), ("driver", "Paul"));
        Assert.Equal(dispatchPath + "?done=closed", (await owner.PostAsync(dispatchPath + "/close", page)).Response.Headers.Location?.OriginalString);
        Guid leg;
        string html = (await owner.GetAsync(dispatchPath)).Html;
        leg = Guid.Parse(Regex.Match(html, dispatchPath + "/legs/(?<leg>[0-9a-f-]{36})/depart").Groups["leg"].Value);
        Assert.Equal(dispatchPath + "?done=departed", (await owner.PostAsync(dispatchPath + "/legs/" + leg + "/depart", page)).Response.Headers.Location?.OriginalString);
        return dispatchPath;
    }

    private async Task<OperationsParcel> ParcelRowAsync(Guid tenant, Guid parcelId)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.AsNoTracking().SingleAsync(item => item.Id == parcelId);
    }

    private async Task<Vixly.Operations.Domain.Dispatch.OperationsDispatch> DispatchRowAsync(Guid tenant, Guid dispatchId)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Dispatches.AsNoTracking().SingleAsync(item => item.Id == dispatchId);
    }

    private async Task<List<OperationsArrivalDiscrepancy>> DiscrepanciesAsync(Guid tenant)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().ArrivalDiscrepancies.AsNoTracking().ToListAsync();
    }

    private async Task<int> OutboxCountAsync(Guid tenant, string type)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().OutboxMessages.AsNoTracking().CountAsync(item => item.Type == type && item.TenantId == tenant);
    }
}
