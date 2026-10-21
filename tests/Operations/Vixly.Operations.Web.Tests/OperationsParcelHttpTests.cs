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
        Assert.Contains("Past expected day (1)", System.Net.WebUtility.HtmlDecode(overdue), StringComparison.Ordinal);
        Assert.Contains("Received (2)", System.Net.WebUtility.HtmlDecode(overdue), StringComparison.Ordinal);

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
        Assert.Contains("To send (1)", list, StringComparison.Ordinal);
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
}
