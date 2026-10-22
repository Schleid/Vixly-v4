using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold;
using Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcel;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Web.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsInspectionHttpTests.cs
//
// PURPOSE:
// Proves compliance inspection and holds through the staff site, on SQL Server (Slice 3C).
//
// RESPONSIBILITIES:
// - A rule, the random share and a clerk's flag send parcels to the queue, with the reason
// - The inspector records outcomes; holds are shown and released with a note
// - A disposal needs the call to the customer and the approval of another person who is the Owner or an administrator
// - A package declared after its check is checked against the rules
//
// IMPORTANT NOTES:
// - The same test host and helpers as OperationsParcelHttpTests (dock, station, customers)
// =========================================================
public sealed partial class OperationsInspectionHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsInspectionHttpTests(OperationsWebDatabase database)
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
    public async Task Rules_SendParcelsToTheQueue_TheInspectorRecordsOutcomes_AndHoldsAreReleased()
    {
        Guid tenant = NewTenant("rulesco", "Rules Co");
        using CookieClient owner = await OwnerAsync("rulesco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456791", "Two lithium batteries and a charger");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456792", "Books");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456793", "Glasses");

        // The company starts from the suggested list (D-R5), turns the random share off and refuses a rule it cannot read.
        (HttpResponseMessage _, string setup) = await owner.GetAsync("/staff/inspection/setup");
        Assert.Contains("Start from the suggested list", setup, StringComparison.Ordinal);
        Assert.Equal("/staff/inspection/setup?done=copied", (await owner.PostAsync("/staff/inspection/setup/starting-list", setup)).Response.Headers.Location?.OriginalString);
        Assert.Equal("/staff/inspection/setup?done=copied", (await owner.PostAsync("/staff/inspection/setup/starting-list", setup)).Response.Headers.Location?.OriginalString);
        Assert.Equal("/staff/inspection/setup?done=saved", (await owner.PostAsync("/staff/inspection/setup/share", setup, ("percent", "0"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string badRule) = await owner.PostAsync("/staff/inspection/setup/rules", setup, ("kind", "DestinationCountry"), ("value", "Haiti"), ("active", "true"));
        Assert.Contains("Enter two-letter country codes", badRule, StringComparison.Ordinal);
        (HttpResponseMessage _, string copied) = await owner.GetAsync("/staff/inspection/setup");
        Assert.Contains("Lithium batteries", copied, StringComparison.Ordinal);
        Assert.Contains("The declaration names an item of the list", copied, StringComparison.Ordinal);
        Assert.Contains("value=\"0\"", Regex.Match(copied, "name=\"percent\"[^>]*").Value, StringComparison.Ordinal);

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        string batteries = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456791")).GetProperty("number").GetString()!;
        string books = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456792")).GetProperty("number").GetString()!;
        string glasses = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456793")).GetProperty("number").GetString()!;

        // At the station: the rule picks the batteries, the clerk flags the books, the glasses go on (the share is 0).
        Assert.Equal("/staff/inspection?done=selected&last=" + batteries, (await StationCheckAsync(owner, batteries)).Headers.Location?.OriginalString);
        (HttpResponseMessage _, string noReason) = await StationCheckWithHtmlAsync(owner, books, string.Empty);
        Assert.Contains("Write why you set this parcel aside.", noReason, StringComparison.Ordinal);
        Assert.Equal("/staff/inspection?done=selected&last=" + books, (await StationCheckAsync(owner, books, flag: "Box rattles")).Headers.Location?.OriginalString);
        Assert.Equal("/staff/inspection?done=checked&last=" + glasses, (await StationCheckAsync(owner, glasses)).Headers.Location?.OriginalString);
        Assert.Equal(0, _host.Draw.LastPercent);

        (HttpResponseMessage _, string queue) = await owner.GetAsync("/staff/inspection/queue");
        Assert.Contains("Declaration names: Lithium batteries", queue, StringComparison.Ordinal);
        Assert.Contains("Set aside by staff: Box rattles", queue, StringComparison.Ordinal);
        Assert.DoesNotContain(glasses, queue, StringComparison.Ordinal);
        Assert.Contains(batteries, queue, StringComparison.Ordinal);
        Assert.Contains(books, queue, StringComparison.Ordinal);

        // Scanning the ID label opens its inspection; the page says what the list asks and that the declaration names it.
        string work = (await owner.GetAsync("/staff/inspection/queue?code=" + batteries)).Response.Headers.Location!.OriginalString;
        Assert.Matches("^/staff/inspection/queue/[0-9a-f-]{36}$", work);
        (HttpResponseMessage _, string waiting) = await owner.GetAsync(work);
        Assert.Contains("named in the declaration", waiting, StringComparison.Ordinal);
        Assert.Contains("no loose spare batteries by air", waiting, StringComparison.Ordinal);
        Assert.Equal(work, (await owner.PostAsync(work + "/start", waiting, ("key", Key(waiting)))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string started) = await owner.GetAsync(work);
        Assert.Contains("Record the outcome", started, StringComparison.Ordinal);

        // Held for the customer: what they must do is required; the customer is told by email.
        (HttpResponseMessage _, string noAction) = await FinishAsync(owner, work, started, ("result", "held"), ("hold", "InspectionDocuments"));
        Assert.Contains("Write what the customer must do.", noAction, StringComparison.Ordinal);
        (HttpResponseMessage held, string _) = await FinishAsync(owner, work, started, ("result", "held"), ("hold", "InspectionDocuments"), ("action", "Send the battery data sheet"),
            ("items", Regex.Match(started, "name=\"items\" value=\"(?<id>[0-9a-f-]{36})\" /> Lithium batteries").Groups["id"].Value));
        Assert.Equal("/staff/inspection/queue?done=finished", held.Headers.Location?.OriginalString);

        Guid parcel = await ParcelIdAsync(tenant, batteries);
        (HttpResponseMessage _, string parcelPage) = await owner.GetAsync("/staff/parcels/" + parcel);
        Assert.Contains("Inspection completed: Held for the customer", parcelPage, StringComparison.Ordinal);
        Assert.Contains("Send the battery data sheet", parcelPage, StringComparison.Ordinal);
        Assert.Contains("On hold: Documents needed", parcelPage, StringComparison.Ordinal);

        // The holds page: what is awaited; a release needs a note.
        (HttpResponseMessage _, string holds) = await owner.GetAsync("/staff/holds?reason=InspectionDocuments");
        Assert.Contains(batteries, holds, StringComparison.Ordinal);
        Assert.Contains("Send the battery data sheet", holds, StringComparison.Ordinal);
        (HttpResponseMessage _, string noNote) = await owner.PostAsync("/staff/holds/" + parcel + "/release", holds, ("reason", "InspectionDocuments"), ("note", " "), ("key", NewKey()));
        Assert.Contains("Write a note: why the hold ends.", noNote, StringComparison.Ordinal);
        (HttpResponseMessage _, string notReleasable) = await owner.PostAsync("/staff/holds/" + parcel + "/release", holds, ("reason", "Unidentified"), ("note", "x"), ("key", NewKey()));
        Assert.Contains("This hold is not released here", notReleasable, StringComparison.Ordinal);
        Assert.Equal("/staff/holds?done=released",
            (await owner.PostAsync("/staff/holds/" + parcel + "/release", holds, ("reason", "InspectionDocuments"), ("note", "Data sheet received by email"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string after) = await owner.GetAsync("/staff/holds");
        Assert.DoesNotContain(batteries, after, StringComparison.Ordinal);

        // The flagged books pass; the queue is empty.
        string booksWork = (await owner.GetAsync("/staff/inspection/queue?code=" + books)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string booksPage) = await owner.GetAsync(booksWork);
        await owner.PostAsync(booksWork + "/start", booksPage, ("key", Key(booksPage)));
        (HttpResponseMessage _, string booksStarted) = await owner.GetAsync(booksWork);
        Assert.Equal("/staff/inspection/queue?done=finished", (await FinishAsync(owner, booksWork, booksStarted, ("result", "passed"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string empty) = await owner.GetAsync("/staff/inspection/queue");
        Assert.Contains("No parcel is waiting for inspection.", empty, StringComparison.Ordinal);

        // A parcel already inspected is not flagged again silently (review of 3C).
        (HttpResponseMessage _, string again) = await StationCheckWithHtmlAsync(owner, books, "Looks different now");
        Assert.Contains("This parcel was already chosen for compliance inspection.", again, StringComparison.Ordinal);

        // Refused for return: the hold ends when it leaves, as "returned to sender".
        Assert.Equal("/staff/inspection?done=selected&last=" + glasses, (await StationCheckAsync(owner, glasses, flag: "Strong smell")).Headers.Location?.OriginalString);
        string glassesWork = (await owner.GetAsync("/staff/inspection/queue?code=" + glasses)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string glassesPage) = await owner.GetAsync(glassesWork);
        await owner.PostAsync(glassesWork + "/start", glassesPage, ("key", Key(glassesPage)));
        (HttpResponseMessage _, string glassesStarted) = await owner.GetAsync(glassesWork);
        await FinishAsync(owner, glassesWork, glassesStarted, ("result", "refused_returned"), ("action", "We return it to the store"));
        Guid glassesId = await ParcelIdAsync(tenant, glasses);
        (HttpResponseMessage _, string refusedHolds) = await owner.GetAsync("/staff/holds?reason=Refused");
        Assert.Contains("Returned to sender", refusedHolds, StringComparison.Ordinal);
        Assert.Contains("We return it to the store", refusedHolds, StringComparison.Ordinal);
        Assert.Equal("/staff/holds?done=released",
            (await owner.PostAsync("/staff/holds/" + glassesId + "/release", refusedHolds, ("reason", "Refused"), ("note", "UPS return label 1Z999"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        OperationsParcel saved = await context.Parcels.SingleAsync(item => item.Id == parcel);
        Assert.Equal(OperationsHoldReasons.None, saved.HoldReasons);
        Assert.False(saved.Flags.HasFlag(OperationsParcelFlags.InspectionPending));
        Assert.Equal(2, await context.OutboxMessages.CountAsync(item => item.StreamId == parcel && item.Type == OperationsReceivingMessages.ParcelNotice));
        Assert.Equal(new[] { "Lithium batteries" }, await context.Inspections.Where(item => item.ParcelId == parcel).Select(item => item.ItemsFound!).ToListAsync());
        Assert.Equal(8, await context.RestrictedItems.CountAsync());
        OperationsParcel back = await context.Parcels.SingleAsync(item => item.Id == glassesId);
        Assert.Equal((OperationsMilestone.ReturnedToSender, OperationsHoldReasons.None), (back.Milestone, back.HoldReasons));
    }

    [SqlServerFact]
    public async Task Disposal_NeedsTheCustomerCall_AndTheApprovalOfAnotherPerson()
    {
        Guid tenant = NewTenant("disposeco", "Dispose Co");
        using CookieClient owner = await OwnerAsync("disposeco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456794", "Fireworks");
        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        string number = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456794")).GetProperty("number").GetString()!;
        Assert.Equal("/staff/inspection?done=selected&last=" + number, (await StationCheckAsync(owner, number, flag: "Smells of gunpowder")).Headers.Location?.OriginalString);

        // An inspector who sees the queue and the holds, but not the company's rules.
        using CookieClient ada = await InspectorAsync(owner, "disposeco");
        (HttpResponseMessage _, string adaHome) = await ada.GetAsync("/staff");
        Assert.Contains("href=\"/staff/inspection/queue\"", adaHome, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/holds\"", adaHome, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/inspection/setup\"", adaHome, StringComparison.Ordinal);
        (HttpResponseMessage _, string adaSetup) = await ada.GetAsync("/staff/inspection/setup");
        Assert.Contains("Your role does not allow this.", adaSetup, StringComparison.Ordinal);

        string work = (await ada.GetAsync("/staff/inspection/queue?code=" + number)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string waiting) = await ada.GetAsync(work);
        await ada.PostAsync(work + "/start", waiting, ("key", Key(waiting)));
        (HttpResponseMessage _, string started) = await ada.GetAsync(work);
        (HttpResponseMessage _, string noCall) = await FinishAsync(ada, work, started, ("result", "refused_disposed"));
        Assert.Contains("record the day you called the customer", noCall, StringComparison.Ordinal);
        Assert.Equal("/staff/inspection/queue?done=finished", (await FinishAsync(ada, work, started, ("result", "refused_disposed"),
            ("calledOn", DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)), ("callNotes", "Customer agreed it cannot travel"))).Response.Headers.Location?.OriginalString);

        // The inspector sees the disposal waiting but cannot decide it; the Owner can.
        Guid inspection = Guid.Parse(work[^36..]);
        (HttpResponseMessage _, string adaHolds) = await ada.GetAsync("/staff/holds");
        Assert.Contains("Customer agreed it cannot travel", adaHolds, StringComparison.Ordinal);
        Assert.Contains("The Owner or an administrator decides.", adaHolds, StringComparison.Ordinal);
        Assert.DoesNotContain("Approve the disposal", adaHolds, StringComparison.Ordinal);
        (HttpResponseMessage _, string refused) = await ada.PostAsync("/staff/holds/disposals/" + inspection, adaHolds, ("decision", "approve"), ("key", NewKey()));
        Assert.Contains("Only the Owner or an administrator decides a disposal.", refused, StringComparison.Ordinal);

        (HttpResponseMessage _, string ownerHolds) = await owner.GetAsync("/staff/holds");
        Assert.Contains("Approve the disposal", ownerHolds, StringComparison.Ordinal);
        Assert.Equal("/staff/holds?done=decided",
            (await owner.PostAsync("/staff/holds/disposals/" + inspection, ownerHolds, ("decision", "approve"), ("note", "Policy 4.2"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        OperationsParcel parcel = await context.Parcels.SingleAsync(item => item.Number == number);
        Assert.Equal((OperationsMilestone.Disposed, OperationsHoldReasons.None), (parcel.Milestone, parcel.HoldReasons));
        OperationsInspection done = await context.Inspections.SingleAsync(item => item.Id == inspection);
        Assert.Equal((OperationsInspectionStatus.Done, "refused_disposed"), (done.Status, done.Result));
        Assert.NotEqual(done.FinishedByUserId, done.ApprovedByUserId);
    }

    // The customer declares a package after it was checked at the station: the rules look at the declaration then (design section 1a).
    [SqlServerFact]
    public async Task PackageDeclaredAfterItsCheck_IsCheckedAgainstTheRules()
    {
        Guid tenant = NewTenant("lateco", "Late Co");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        IServiceProvider services = scope.ServiceProvider;
        OperationsDbContext context = services.GetRequiredService<OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        context.Add(OperationsInspectionRule.Create(tenant, OperationsInspectionRuleKind.ListedItems, null, now));
        context.Add(OperationsRestrictedItem.Create(tenant, "Lithium batteries", "lithium, battery", OperationsRestrictedItemKind.Restricted, null, null, null, now));

        // Arrived unannounced, checked at the station, waiting in the customer's shipment without instructions.
        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(tenant, warehouse, "UPS", Guid.NewGuid(), NewKey(), now);
        context.Add(delivery);
        OperationsParcel parcel = OperationsParcel.Receive(tenant, "PK26" + Random.Shared.Next(100000, 999999), delivery, "UPS", "1Z999AA10123456795", false, now);
        OperationsShipment waiting = OperationsShipment.Open(tenant, lee, warehouse, now);
        parcel.JoinShipment(waiting, now);
        context.AddRange(parcel, waiting);
        OperationsTracker tracker = services.GetRequiredService<OperationsTracker>();
        foreach (string code in new[] { OperationsTrackingCodes.ReceivedAtOrigin, OperationsTrackingCodes.Checked })
        {
            Dictionary<string, string> arguments = code == OperationsTrackingCodes.ReceivedAtOrigin
                ? new(StringComparer.Ordinal) { ["warehouse"] = "WH1" }
                : new(StringComparer.Ordinal) { ["weight"] = "1", ["size"] = "10 x 10 x 10" };
            Assert.Null((await tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments, now, NewKey(),
                OperationsTrackingActor.Staff, OperationsTrackingSource.Scan) { ActorUserId = Guid.NewGuid() }, CancellationToken.None)).Error);
        }

        OperationsShipment instructed = OperationsShipment.Request(tenant, lee, warehouse, OperationsShipmentInstructions.HomeDelivery(
            OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), "Ana Pierre", "+50937123456", OperationsShipmentOptions.None), now);
        context.Add(instructed);
        Assert.True(await services.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));

        (OperationsPreAlert? announced, OperationsParcel? matched, Vixly.Shared.Kernel.Errors.Error? error) = await services.GetRequiredService<OperationsPreAlertIntake>()
            .AnnounceAsync(instructed, new OperationsPackageDraft("UPS", "1Z999AA10123456795", null, "A laptop battery", 80m, null), DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.Null(error);
        Assert.NotNull(announced);
        Assert.True(await services.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));

        OperationsInspection inspection = await context.Inspections.AsNoTracking().SingleAsync(item => item.ParcelId == parcel.Id);
        Assert.Equal(("rule", "listed:Lithium batteries"), (inspection.Why, inspection.Reason));
        Assert.True(matched!.Flags.HasFlag(OperationsParcelFlags.InspectionPending));
    }

    // A double click that gets past the browser guard: each inspection form twice at the same moment shows its result, never an error (ADR-0019).
    [SqlServerFact]
    public async Task InspectionForms_SentTwiceAtOnce_ShowTheirResult()
    {
        Guid tenant = NewTenant("twiceinsp", "Twice Inspect");
        using CookieClient owner = await OwnerAsync("twiceinsp");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456796", "A lithium battery");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456797", "Fireworks");
        List<string> failures = new();
        (HttpResponseMessage _, string setup) = await owner.GetAsync("/staff/inspection/setup");

        await BothRedirectAsync(failures, "starting list", () => owner.PostAsync("/staff/inspection/setup/starting-list", setup));
        await BothRedirectAsync(failures, "share", () => owner.PostAsync("/staff/inspection/setup/share", setup, ("percent", "0")));
        await BothRedirectAsync(failures, "rule", () => owner.PostAsync("/staff/inspection/setup/rules", setup, ("kind", "ValueAtLeast"), ("value", "900"), ("active", "true")));
        await BothRedirectAsync(failures, "item", () => owner.PostAsync("/staff/inspection/setup/items", setup, ("name", "Drones"), ("words", "drone, drones"), ("kind", "Restricted"), ("active", "true")));

        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        string battery = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456796")).GetProperty("number").GetString()!;
        string fireworks = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456797")).GetProperty("number").GetString()!;
        await StationCheckAsync(owner, battery);
        await StationCheckAsync(owner, fireworks, flag: "Smells of gunpowder");

        string work = (await owner.GetAsync("/staff/inspection/queue?code=" + battery)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string waiting) = await owner.GetAsync(work);
        await BothRedirectAsync(failures, "start", () => owner.PostAsync(work + "/start", waiting, ("key", Key(waiting))));
        (HttpResponseMessage _, string started) = await owner.GetAsync(work);
        await BothRedirectAsync(failures, "finish", () => FinishAsync(owner, work, started, ("result", "held"), ("hold", "InspectionValue"), ("action", "Send the receipt")));

        Guid parcel = await ParcelIdAsync(tenant, battery);
        (HttpResponseMessage _, string holds) = await owner.GetAsync("/staff/holds");
        string release = NewKey();
        await BothRedirectAsync(failures, "release", () => owner.PostAsync("/staff/holds/" + parcel + "/release", holds, ("reason", "InspectionValue"), ("note", "Receipt received"), ("key", release)));

        using CookieClient ada = await InspectorAsync(owner, "twiceinsp");
        string disposal = (await ada.GetAsync("/staff/inspection/queue?code=" + fireworks)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string adaWaiting) = await ada.GetAsync(disposal);
        await ada.PostAsync(disposal + "/start", adaWaiting, ("key", Key(adaWaiting)));
        (HttpResponseMessage _, string adaStarted) = await ada.GetAsync(disposal);
        await FinishAsync(ada, disposal, adaStarted, ("result", "refused_disposed"), ("calledOn", DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)), ("callNotes", "Agreed"));
        (HttpResponseMessage _, string ownerHolds) = await owner.GetAsync("/staff/holds");
        string decision = NewKey();
        await BothRedirectAsync(failures, "disposal", () => owner.PostAsync("/staff/holds/disposals/" + disposal[^36..], ownerHolds, ("decision", "approve"), ("key", decision)));

        Assert.True(failures.Count == 0, string.Join("\n", failures));

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        Assert.Equal(9, await context.RestrictedItems.CountAsync());
        Assert.Equal(2, await context.InspectionRules.CountAsync());
        Assert.Equal(1, await context.TrackingEvents.CountAsync(item => item.SubjectId == parcel && item.Code == OperationsTrackingCodes.HoldReleased));
        Assert.Equal(1, await context.TrackingEvents.CountAsync(item => item.Code == OperationsTrackingCodes.Disposed));
    }

    // Slice 3E: the customer answers holds online; staff review the answers; reminders go out; unclaimed packages are decided.
    [SqlServerFact]
    public async Task CustomerAnswers_AreReviewed_RemindersGoOut_AndUnclaimedPackagesAreDecided()
    {
        Guid tenant = NewTenant("answerco", "Answer Co");
        using CookieClient owner = await OwnerAsync("answerco");
        Guid warehouse = await WarehouseAsync(tenant);
        (Guid lee, string _) = await CustomerAsync(tenant, "lee@example.com");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456801", "Phones");
        await PreAlertAsync(tenant, lee, "1Z999AA10123456802", "Lamp");
        (string station, Guid delivery) = await OpenAsync(owner, warehouse, "UPS");
        string phones = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456801")).GetProperty("number").GetString()!;
        string lamp = (await ScanOkAsync(owner, station, delivery, "1Z999AA10123456802", damaged: true)).GetProperty("number").GetString()!;
        string stray = (await ScanOkAsync(owner, station, delivery, "TBA0009990001")).GetProperty("number").GetString()!;
        await StationCheckAsync(owner, phones, flag: "Value looks low");
        string work = (await owner.GetAsync("/staff/inspection/queue?code=" + phones)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string waiting) = await owner.GetAsync(work);
        await owner.PostAsync(work + "/start", waiting, ("key", Key(waiting)));
        (HttpResponseMessage _, string started) = await owner.GetAsync(work);
        await FinishAsync(owner, work, started, ("result", "held"), ("hold", "InspectionValue"), ("action", "Confirm the value with the invoice"));
        Guid phonesId = await ParcelIdAsync(tenant, phones);
        Guid lampId = await ParcelIdAsync(tenant, lamp);

        // Reminders: the first one at day 7, once; the final notice at day 25, alone even if the job missed the others.
        await AgeAsync(tenant, phonesId, 8);
        OperationsUnclaimedWorker reminders = _host.Services.GetRequiredService<OperationsUnclaimedWorker>();
        Assert.Equal(1, await reminders.RunAsync(CancellationToken.None));
        Assert.Equal(0, await reminders.RunAsync(CancellationToken.None));
        await AgeAsync(tenant, phonesId, 26);
        Assert.Equal(1, await reminders.RunAsync(CancellationToken.None));
        Assert.Equal(new[] { OperationsCustomerMailKind.ParcelReminder, OperationsCustomerMailKind.ParcelFinalNotice },
            (await NoticesAsync(phonesId)).Where(kind => kind is OperationsCustomerMailKind.ParcelReminder or OperationsCustomerMailKind.ParcelFinalNotice).ToArray());
        await AgeAsync(tenant, phonesId, 0);

        // The customer answers: the value, then (declined) again; and for the damaged lamp, disposal.
        string session = await SessionAsync(tenant, lee);
        Assert.Equal("ops.holds.document_invalid", (await AnswerAsync(tenant, session, phonesId, OperationsHoldReasons.InspectionValue, contents: "x", value: 1m, document: "not a pdf"u8.ToArray())).Error?.Code);
        Assert.True((await AnswerAsync(tenant, session, phonesId, OperationsHoldReasons.InspectionValue, contents: "Two used phones", value: 300m, document: Pdf())).Succeeded);
        Assert.True((await AnswerAsync(tenant, session, lampId, OperationsHoldReasons.Damaged, choice: "dispose")).Succeeded);

        (HttpResponseMessage _, string holds) = await owner.GetAsync("/staff/holds");
        Assert.Contains("Two used phones", holds, StringComparison.Ordinal);
        Assert.Contains("Open the document (invoice.pdf)", holds, StringComparison.Ordinal);
        Guid valueAnswer = Guid.Parse(Regex.Match(holds, "/staff/holds/answers/(?<id>[0-9a-f-]{36})/document").Groups["id"].Value);
        (HttpResponseMessage document, string _) = await owner.GetAsync("/staff/holds/answers/" + valueAnswer + "/document");
        Assert.Equal("application/pdf", document.Content.Headers.ContentType?.MediaType);
        Assert.Contains("sandbox", document.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);

        Assert.Equal("/staff/holds?done=reviewed",
            (await owner.PostAsync("/staff/holds/answers/" + valueAnswer, holds, ("decision", "decline"), ("note", "The invoice is unreadable"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);
        GetMyOperationsParcelResponse seen = await CustomerViewAsync(tenant, session, phonesId);
        Assert.Equal("The invoice is unreadable", seen.Answers.Single(answer => answer.Reason == OperationsHoldReasons.InspectionValue).DeclinedBecause);
        Assert.Contains(OperationsHoldReasons.InspectionValue, seen.Answerable);
        Assert.True((await AnswerAsync(tenant, session, phonesId, OperationsHoldReasons.InspectionValue, contents: "Two used phones", value: 320m, document: Pdf())).Succeeded);

        // An inspector cannot accept a disposal; the Owner can, and accepting the value corrects the declaration.
        using CookieClient ada = await InspectorAsync(owner, "answerco");
        (HttpResponseMessage _, string adaHolds) = await ada.GetAsync("/staff/holds");
        Match[] forms = Regex.Matches(adaHolds, "action=\"/staff/holds/answers/(?<id>[0-9a-f-]{36})\"(?<form>.*?)</form>", RegexOptions.Singleline).ToArray();
        Guid disposeAnswer = Guid.Parse(forms.Single(form => !form.Groups["form"].Value.Contains("value=\"accept\"", StringComparison.Ordinal)).Groups["id"].Value);
        (HttpResponseMessage _, string refused) = await ada.PostAsync("/staff/holds/answers/" + disposeAnswer, adaHolds, ("decision", "accept"), ("key", NewKey()));
        Assert.Contains("Only the Owner or an administrator", refused, StringComparison.Ordinal);
        (HttpResponseMessage _, string ownerHolds) = await owner.GetAsync("/staff/holds");
        Assert.NotEqual(Guid.Empty, disposeAnswer);
        Assert.Equal("/staff/holds?done=reviewed",
            (await owner.PostAsync("/staff/holds/answers/" + disposeAnswer, ownerHolds, ("decision", "accept"), ("note", "Customer chose disposal"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);

        // The stray package, unidentified for 35 days, is decided by the Owner.
        Guid strayId = await ParcelIdAsync(tenant, stray);
        await AgeAsync(tenant, strayId, 35);
        (HttpResponseMessage _, string unclaimed) = await owner.GetAsync("/staff/holds");
        Assert.Contains("Unclaimed after 30 days", unclaimed, StringComparison.Ordinal);
        Assert.Contains(stray, Regex.Match(unclaimed, "id=\"unclaimed-title\".*?</section>", RegexOptions.Singleline).Value, StringComparison.Ordinal);
        Assert.Equal("/staff/holds?done=decided",
            (await owner.PostAsync("/staff/holds/unclaimed/" + strayId, unclaimed, ("decision", "return"), ("note", "No owner found"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string notYet) = await owner.PostAsync("/staff/holds/unclaimed/" + phonesId, unclaimed, ("decision", "dispose"), ("key", NewKey()));
        Assert.Contains("This package is not unclaimed", notYet, StringComparison.Ordinal);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        OperationsParcel lampSaved = await context.Parcels.SingleAsync(item => item.Id == lampId);
        Assert.Equal((OperationsMilestone.Disposed, OperationsHoldReasons.None), (lampSaved.Milestone, lampSaved.HoldReasons));
        OperationsParcel straySaved = await context.Parcels.SingleAsync(item => item.Id == strayId);
        Assert.Equal((OperationsMilestone.ReturnedToSender, OperationsHoldReasons.None), (straySaved.Milestone, straySaved.HoldReasons));

        (HttpResponseMessage _, string last) = await owner.GetAsync("/staff/holds");
        Guid newest = Guid.Parse(Regex.Match(last, "/staff/holds/answers/(?<id>[0-9a-f-]{36})/document").Groups["id"].Value);
        Assert.Equal("/staff/holds?done=reviewed", (await owner.PostAsync("/staff/holds/answers/" + newest, last, ("decision", "accept"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);
        OperationsParcel phonesSaved = await context.Parcels.AsNoTracking().SingleAsync(item => item.Id == phonesId);
        Assert.Equal(("Two used phones", 320m, OperationsHoldReasons.None), (phonesSaved.DeclaredContents, phonesSaved.DeclaredValue!.Value, phonesSaved.HoldReasons & OperationsHoldReasons.InspectionValue));

        // A hold released by hand while the customer's answer waited: the answer has nothing left to decide.
        await PreAlertAsync(tenant, lee, "1Z999AA10123456803", "Chair");
        (string station2, Guid delivery2) = await OpenAsync(owner, warehouse, "UPS");
        string chair = (await ScanOkAsync(owner, station2, delivery2, "1Z999AA10123456803", damaged: true)).GetProperty("number").GetString()!;
        Guid chairId = await ParcelIdAsync(tenant, chair);
        Assert.True((await AnswerAsync(tenant, session, chairId, OperationsHoldReasons.Damaged, choice: "return")).Succeeded);
        (HttpResponseMessage _, string withChair) = await owner.GetAsync("/staff/holds");
        Guid chairAnswer = Guid.Parse(Regex.Match(withChair, "action=\"/staff/holds/answers/(?<id>[0-9a-f-]{36})\"").Groups["id"].Value);
        Assert.Equal("/staff/holds?done=released",
            (await owner.PostAsync("/staff/holds/" + chairId + "/release", withChair, ("reason", "Damaged"), ("note", "Only the box was damaged"), ("key", NewKey()))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string stale) = await owner.PostAsync("/staff/holds/answers/" + chairAnswer, withChair, ("decision", "accept"), ("key", NewKey()));
        Assert.Contains("This hold has already ended", stale, StringComparison.Ordinal);
        Assert.NotEqual(OperationsMilestone.ReturnedToSender, (await context.Parcels.AsNoTracking().SingleAsync(item => item.Id == chairId)).Milestone);
    }

    // Moves a parcel's receipt into the past, as if it had waited that many days.
    private async Task AgeAsync(Guid tenant, Guid parcel, int days)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        DateTimeOffset received = DateTimeOffset.UtcNow.AddDays(-days).AddMinutes(-5);
        await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.Where(item => item.Id == parcel)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedUtc, received));
    }

    private async Task<IReadOnlyList<OperationsCustomerMailKind>> NoticesAsync(Guid parcel)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
        return (await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().OutboxMessages.Where(item => item.StreamId == parcel && item.Type == OperationsReceivingMessages.ParcelNotice)
                .OrderBy(item => item.CreatedUtc).ToListAsync())
            .Select(message => OperationsOutboxWriter.Read<OperationsParcelNotice>(message).Kind).ToList();
    }

    private async Task<string> SessionAsync(Guid tenant, Guid customer)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsCustomer found = (await scope.ServiceProvider.GetRequiredService<IOperationsCustomerRepository>().GetAsync(customer, CancellationToken.None))!;
        string token = scope.ServiceProvider.GetRequiredService<OperationsCustomerAccounts>().StartSession(found, DateTimeOffset.UtcNow);
        await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        return token;
    }

    private async Task<AnswerMyOperationsHoldResponse> AnswerAsync(Guid tenant, string session, Guid parcel, OperationsHoldReasons reason, string? contents = null, decimal? value = null,
        string? choice = null, byte[]? document = null)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<AnswerMyOperationsHoldUseCase>().ExecuteAsync(OperationsAuthorizationContext.ForTenant(tenant), new AnswerMyOperationsHoldRequest
        {
            SessionToken = session,
            ParcelId = parcel,
            Reason = reason,
            Contents = contents,
            Value = value,
            Choice = choice,
            Document = document,
            FileName = document is null ? null : "invoice.pdf"
        }, CancellationToken.None);
    }

    private async Task<GetMyOperationsParcelResponse> CustomerViewAsync(Guid tenant, string session, Guid parcel)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<GetMyOperationsParcelUseCase>().ExecuteAsync(OperationsAuthorizationContext.ForTenant(tenant),
            new GetMyOperationsParcelRequest { SessionToken = session, ParcelId = parcel }, CancellationToken.None);
    }

    private static byte[] Pdf() => "%PDF-1.4\n1 0 obj << >> endobj\ntrailer << >>\n%%EOF"u8.ToArray();

    // Both answers are the outcome page (a redirect), never an error shown on the form.
    private static async Task BothRedirectAsync(List<string> failures, string name, Func<Task<(HttpResponseMessage Response, string Html)>> send)
    {
        foreach ((HttpResponseMessage response, string html) in await Task.WhenAll(send(), send()))
        {
            if (response.StatusCode != HttpStatusCode.Redirect)
            {
                failures.Add(name + ": " + (int)response.StatusCode + " " + Regex.Match(html, "role=\"alert\"[^>]*>(?<m>[^<]*)").Groups["m"].Value);
            }
        }
    }

    // The inspection station form for a scanned parcel number; a flag sets it aside with a reason.
    private async Task<HttpResponseMessage> StationCheckAsync(CookieClient clerk, string number, string? flag = null) =>
        (await StationCheckWithHtmlAsync(clerk, number, flag)).Response;

    private async Task<(HttpResponseMessage Response, string Html)> StationCheckWithHtmlAsync(CookieClient clerk, string number, string? flag)
    {
        string path = (await clerk.GetAsync("/staff/inspection?code=" + number)).Response.Headers.Location!.OriginalString;
        (HttpResponseMessage _, string form) = await clerk.GetAsync(path);
        List<(string, string)> fields = new()
        {
            ("key", Key(form)), ("condition", "intact"), ("weight", "2.5"), ("length", "30"), ("width", "20"), ("height", "10.5")
        };

        if (flag is not null)
        {
            fields.Add(("flag", "true"));
            fields.Add(("flagReason", flag));
        }

        return await clerk.PostFilesAsync(path, form, new[] { ("photo_package", "package.png", Png()) }, fields.ToArray());
    }

    private static Task<(HttpResponseMessage Response, string Html)> FinishAsync(CookieClient inspector, string work, string page, params (string Name, string Value)[] fields) =>
        inspector.PostFilesAsync(work + "/finish", page, Array.Empty<(string, string, byte[])>(), fields.Append(("key", Key(page))).ToArray());

    private static string Key(string page) => Regex.Match(page, "name=\"key\" value=\"(?<key>[0-9a-f]{32})\"").Groups["key"].Value;

    private async Task<Guid> ParcelIdAsync(Guid tenant, string number)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        return await scope.ServiceProvider.GetRequiredService<OperationsDbContext>().Parcels.Where(item => item.Number == number).Select(item => item.Id).SingleAsync();
    }

    // Ada, invited by the Owner as an inspector for the whole company.
    private async Task<CookieClient> InspectorAsync(CookieClient owner, string slug)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, ("name", "Inspector"), ("permissions", "ops.parcels.view"), ("permissions", "ops.inspection.check"),
            ("permissions", "ops.inspection.perform"), ("permissions", "ops.holds.manage"));
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
