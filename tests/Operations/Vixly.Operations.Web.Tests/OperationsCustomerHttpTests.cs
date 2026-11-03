using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Web.Hosting;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsCustomerHttpTests.cs
//
// PURPOSE:
// Proves the staff customer pages over HTTP with a real database (Slice 2, design section 3).
//
// RESPONSIBILITIES:
// - Self-registered customers are listed and found; staff cannot create or edit them (owner, 2026-10-18)
// - Status with a reason, credit, history
// - Staff without ops.customers.* see nothing
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER
// =========================================================
public sealed partial class OperationsCustomerHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsCustomerHttpTests(OperationsWebDatabase database)
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
    public async Task Owner_SeesSelfRegisteredCustomers_AndLooksAfterThem_ButNeverEntersTheirDetails()
    {
        Guid tenant = NewTenant("custco", "Cust Co");
        using CookieClient owner = await OwnerAsync("custco");

        (HttpResponseMessage _, string empty) = await owner.GetAsync("/staff/customers");
        Assert.Contains("No customer found.", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("/staff/customers/create", empty, StringComparison.Ordinal);

        // The menu: four chapters (owner, 2026-10-22); billing and Terms are tabs of Company, not menu items of their own (owner, 2026-10-18).
        Assert.Contains(">Front Office<", empty, StringComparison.Ordinal);
        Assert.Contains(">Warehouse Operations<", empty, StringComparison.Ordinal);
        Assert.Contains(">Back Office<", empty, StringComparison.Ordinal);
        Assert.Contains(">Technical<", empty, StringComparison.Ordinal);
        Assert.Contains(">Customer drop-off<", empty, StringComparison.Ordinal);
        Assert.Contains(">Warehouse reception<", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("Team and access", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/billing\"", empty, StringComparison.Ordinal);
        (HttpResponseMessage _, string company) = await owner.GetAsync("/staff/company");
        Assert.Contains("href=\"/staff/billing\"", company, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/legal\"", company, StringComparison.Ordinal);

        Guid customerId = await RegisteredCustomerAsync(tenant, "ana@example.com", "C00001");
        string customerPath = "/staff/customers/" + customerId;

        (HttpResponseMessage _, string found) = await owner.GetAsync("/staff/customers?search=C00001");
        Assert.Contains("Ana Pierre", found, StringComparison.Ordinal);
        (HttpResponseMessage _, string page) = await owner.GetAsync(customerPath);
        Assert.Contains("+509 3712-3456", page, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"firstName\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("/invitation", page, StringComparison.Ordinal);

        // No route takes customer details from staff (owner, 2026-10-18).
        (HttpResponseMessage create, string _) = await owner.PostAsync("/staff/customers/create", page,
            ("firstName", "Eve"), ("lastName", "Staff"), ("email", "eve@example.com"), ("phone", "+50937123456"), ("language", "en"));
        (HttpResponseMessage edit, string _) = await owner.PostAsync(customerPath, page, ("firstName", "Changed"), ("lastName", "Name"), ("phone", "+50937123456"), ("language", "en"));
        Assert.True(create.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
        Assert.True(edit.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);

        (HttpResponseMessage _, string noReason) = await owner.PostAsync(customerPath + "/status", page, ("status", "suspended"), ("reason", " "));
        Assert.Contains("Enter a reason.", noReason, StringComparison.Ordinal);
        (HttpResponseMessage suspended, string _) = await owner.PostAsync(customerPath + "/status", page, ("status", "suspended"), ("reason", "Unpaid shipments"));
        Assert.Equal(customerPath + "?done=status", suspended.Headers.Location?.OriginalString);
        // Slice 5F step 2b: credit is the credit department's account now; the old toggle is gone.
        (HttpResponseMessage credit, string _) = await owner.PostAsync(customerPath + "/credit", page, ("allowed", "true"), ("reason", "Good customer since 2019"));
        Assert.True(credit.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);

        (HttpResponseMessage _, string after) = await owner.GetAsync(customerPath);
        Assert.Contains("Ana Pierre", after, StringComparison.Ordinal);
        Assert.Contains("Suspended", after, StringComparison.Ordinal);
        Assert.Contains("Unpaid shipments", after, StringComparison.Ordinal);
        // Owner, 2026-10-31: no credit account from the customer page; the status panel and the customer's shipments are there.
        Assert.DoesNotContain("/staff/credit/customers/", after, StringComparison.Ordinal);
        Assert.Contains("action=\"" + customerPath + "/status\"", after, StringComparison.Ordinal);
        Assert.Contains("value=\"banned\"", after, StringComparison.Ordinal);
        Assert.Contains("id=\"customer-shipments\"", after, StringComparison.Ordinal);
        Assert.Contains(_host.CustomerMailer.Sent, mail => mail.Kind == OperationsCustomerMailKind.AccountSuspended && mail.ToAddress == "ana@example.com");

        // A suspension that ends by itself; then reactivated by hand, closed, reopened and banned for good.
        string day = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(customerPath + "?done=status", (await owner.PostAsync(customerPath + "/status", after, ("status", "suspended"), ("reason", "Check documents"), ("until", day)))
            .Response.Headers.Location?.OriginalString);
        Assert.Contains("Suspended until " + day, (await owner.GetAsync(customerPath)).Html, StringComparison.Ordinal);
        Assert.Contains(_host.CustomerMailer.Sent, mail => mail.Kind == OperationsCustomerMailKind.AccountSuspendedUntil);
        foreach ((string status, string reason) in new[] { ("active", "Documents received"), ("closed", "Asked to close"), ("active", "Came back"), ("banned", "Fraud") })
        {
            Assert.Equal(customerPath + "?done=status", (await owner.PostAsync(customerPath + "/status", after, ("status", status), ("reason", reason))).Response.Headers.Location?.OriginalString);
        }

        string banned = (await owner.GetAsync(customerPath)).Html;
        Assert.Contains("This customer is banned.", banned, StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"" + customerPath + "/status\"", banned, StringComparison.Ordinal);
        Assert.Contains(_host.CustomerMailer.Sent, mail => mail.Kind == OperationsCustomerMailKind.AccountBanned);

        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=customers");
        Assert.Contains("ops.customers.status_changed", history, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task StaffWithoutCustomerPermissions_SeeNothing()
    {
        Guid tenant = NewTenant("closedco", "Closed Co");
        using CookieClient owner = await OwnerAsync("closedco");
        string customerPath = "/staff/customers/" + await RegisteredCustomerAsync(tenant, "secret@example.com", "C00001");

        using CookieClient ada = await StaffAsync(owner, "closedco");

        (HttpResponseMessage _, string refusedList) = await ada.GetAsync("/staff/customers");
        Assert.Contains("Your role does not allow this.", refusedList, StringComparison.Ordinal);
        Assert.DoesNotContain("secret@example.com", refusedList, StringComparison.Ordinal);

        (HttpResponseMessage _, string refusedPage) = await ada.GetAsync(customerPath);
        Assert.Contains("Your role does not allow this.", refusedPage, StringComparison.Ordinal);
        Assert.DoesNotContain("secret@example.com", refusedPage, StringComparison.Ordinal);
    }

    // Owner, 2026-10-29 (design 4.4, R-4): staff with the lookup find the one customer they serve by an exact detail; never the list; every lookup is recorded.
    [SqlServerFact]
    public async Task StaffWithTheLookup_FindOneCustomerByAnExactDetail_NeverTheList()
    {
        Guid tenant = NewTenant("deskco", "Desk Co");
        using CookieClient owner = await OwnerAsync("deskco");
        Guid customerId = await RegisteredCustomerAsync(tenant, "jean@example.com", "C00001");
        await RegisteredCustomerAsync(tenant, "marie@example.com", "C00002");

        using CookieClient ada = await StaffAsync(owner, "deskco", "Desk", "ops.customers.lookup");

        (HttpResponseMessage _, string page) = await ada.GetAsync("/staff/customers");
        Assert.Contains("Find a customer", page, StringComparison.Ordinal);
        Assert.DoesNotContain("jean@example.com", page, StringComparison.Ordinal);
        Assert.DoesNotContain("marie@example.com", page, StringComparison.Ordinal);

        (HttpResponseMessage _, string byName) = await ada.GetAsync("/staff/customers?search=Pierre");
        Assert.Contains("No customer has exactly this phone number, email or code.", System.Net.WebUtility.HtmlDecode(byName), StringComparison.Ordinal);

        HttpResponseMessage found = (await ada.GetAsync("/staff/customers?search=jean%40example.com")).Response;
        Assert.Equal("/staff/customers/" + customerId, found.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string customer) = await ada.GetAsync("/staff/customers/" + customerId);
        Assert.Contains("jean@example.com", customer, StringComparison.Ordinal);
        Assert.DoesNotContain("Your role does not allow this.", customer, StringComparison.Ordinal);

        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=lookups");
        Assert.Contains("ops.lookups.customer", history, StringComparison.Ordinal);
        Assert.DoesNotContain("jean@example.com", history, StringComparison.Ordinal);
    }

    // Owner, 2026-10-29 (design section 9): an outside support agent finds the caller by phone, then opens only the caller's shipment by its tracking number,
    // sees amounts and masked contact, opens a call ticket for staff; without a caller, the status only; outside its days, nothing.
    [SqlServerFact]
    public async Task SupportAgent_PhoneThenTrackingNumber_SeesOnlyTheCallersShipment_AndOpensACallTicket()
    {
        Guid tenant = NewTenant("callco", "Call Co");
        using CookieClient owner = await OwnerAsync("callco");
        Guid jean = await RegisteredCustomerAsync(tenant, "jean@example.com", "C00001", "+50937123456");
        Guid marie = await RegisteredCustomerAsync(tenant, "marie@example.com", "C00002", "+50948605107");
        string jeans = Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.Make("VX", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 });
        string maries = Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.Make("VX", new[] { 0, 9, 8, 7, 6, 5, 4, 3, 2, 1 });
        await ShipmentAsync(tenant, (jean, jeans), (marie, maries));

        using CookieClient ada = await StaffAsync(owner, "callco", "Calls", "ops.support.lookup");
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string adaId = Regex.Match(people, "/staff/people/(?<id>[0-9a-f-]{36})\"[^>]*>Ada Lovelace").Groups["id"].Value;
        (HttpResponseMessage _, string member) = await owner.GetAsync("/staff/people/" + adaId);
        (HttpResponseMessage outside, string _) = await owner.PostAsync("/staff/people/" + adaId + "/outside", member,
            ("outside", "true"), ("days", "1"), ("days", "2"), ("days", "4"), ("days", "8"), ("days", "16"), ("days", "32"), ("days", "64"));
        Assert.Equal("/staff/people/" + adaId + "?done=outside", outside.Headers.Location?.OriginalString);

        // An outside account holds nothing but the support lookup.
        (HttpResponseMessage _, string start) = await ada.GetAsync("/staff/support");
        Assert.Contains("Find the caller", start, StringComparison.Ordinal);
        (HttpResponseMessage _, string list) = await ada.GetAsync("/staff/customers");
        Assert.DoesNotContain("jean@example.com", list, StringComparison.Ordinal);

        // Step 1: the phone, typed another way, names Jean briefly.
        (HttpResponseMessage _, string caller) = await ada.PostAsync("/staff/support/caller", start, ("phone", "509 3712-3456"));
        caller = WebUtility.HtmlDecode(caller);
        Assert.Contains("Ana P.", caller, StringComparison.Ordinal);
        Assert.Contains("Good standing", caller, StringComparison.Ordinal);
        Assert.DoesNotContain("jean@example.com", caller, StringComparison.Ordinal);
        string token = Regex.Match(caller, "name=\"caller\" value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        Assert.NotEmpty(token);

        // Marie's shipment is not Jean's: nothing, and recorded.
        (HttpResponseMessage _, string notTheirs) = await ada.PostAsync("/staff/support/case", caller, ("caller", token), ("number", maries));
        Assert.Contains("Nothing found for this number for this caller.", WebUtility.HtmlDecode(notTheirs), StringComparison.Ordinal);
        Assert.DoesNotContain(jeans, notTheirs, StringComparison.Ordinal);

        // Jean's own: masked contact, never the email or the full phone.
        (HttpResponseMessage _, string own) = await ada.PostAsync("/staff/support/case", caller, ("caller", token), ("number", jeans.ToLowerInvariant()));
        own = WebUtility.HtmlDecode(own);
        Assert.Contains("Not invoiced yet", own, StringComparison.Ordinal);
        Assert.Contains("j\u2022\u2022\u2022@e\u2022\u2022\u2022.com", own, StringComparison.Ordinal);
        Assert.DoesNotContain("jean@example.com", own, StringComparison.Ordinal);
        Assert.DoesNotContain("37123456", own, StringComparison.Ordinal);

        // A forged caller opens nothing more than the status.
        (HttpResponseMessage _, string forged) = await ada.PostAsync("/staff/support/case", caller, ("caller", "not-a-token"), ("number", jeans));
        Assert.Contains("Status only", WebUtility.HtmlDecode(forged), StringComparison.Ordinal);
        Assert.DoesNotContain("Amount due", forged, StringComparison.Ordinal);

        // A carrier's number needs the phone first.
        (HttpResponseMessage _, string carrier) = await ada.PostAsync("/staff/support/case", caller, ("number", "1Z999AA10123456784"));
        Assert.Contains("A carrier's tracking number needs the caller's phone number first.", WebUtility.HtmlDecode(carrier), StringComparison.Ordinal);

        // A call ticket for staff.
        (HttpResponseMessage opened, string _) = await ada.PostAsync("/staff/support/request", own, ("caller", token), ("number", jeans), ("text", "Change the pickup person to his mother"));
        Assert.Equal("/staff/support?done=request", opened.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string workspace) = await owner.GetAsync("/staff");
        Assert.Contains("Open customer service tickets", WebUtility.HtmlDecode(workspace), StringComparison.Ordinal);

        // Owner, 2026-10-29 (ticket desk): the call ticket is a ticket of the desk, for customer service, never the customer's.
        (HttpResponseMessage _, string desk) = await owner.GetAsync("/staff/tickets");
        Assert.Contains("Support call", desk, StringComparison.Ordinal);
        string ticket = Regex.Match(desk, "/staff/tickets/(?<id>[0-9a-f-]{36})\"").Groups["id"].Value;
        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/tickets/" + ticket);
        Assert.Contains("Change the pickup person to his mother", page, StringComparison.Ordinal);
        Assert.Contains("Ada Lovelace", page, StringComparison.Ordinal);
        Assert.Equal("/staff/tickets/" + ticket + "?done=taken", (await owner.PostAsync("/staff/tickets/" + ticket + "/take", page)).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string taken) = await owner.GetAsync("/staff/tickets/" + ticket);
        Assert.Contains("Release it", taken, StringComparison.Ordinal);
        Assert.Equal("/staff/tickets/" + ticket + "?done=resolved", (await owner.PostAsync("/staff/tickets/" + ticket + "/answer", taken,
            ("message", "Pickup person changed"), ("resolve", "true"), ("key", Guid.NewGuid().ToString("N")))).Response.Headers.Location?.OriginalString);
        Assert.Equal(0, await OutboxCountAsync(Vixly.Operations.Application.Tickets.OperationsTicketNotice.MessageType, Guid.Parse(ticket)));

        // Every lookup is recorded, never the phone typed.
        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=calls");
        Assert.Contains("ops.calls.tracking", history, StringComparison.Ordinal);
        Assert.Contains("ops.calls.caller", history, StringComparison.Ordinal);
        Assert.DoesNotContain("3712-3456", history, StringComparison.Ordinal);

        // Outside its days (only tomorrow), the account does nothing.
        int tomorrow = Vixly.Operations.Domain.Identity.OperationsOutsideHours.DayBit(DateTime.UtcNow.AddDays(1).DayOfWeek);
        (HttpResponseMessage _, string again) = await owner.GetAsync("/staff/people/" + adaId);
        await owner.PostAsync("/staff/people/" + adaId + "/outside", again, ("outside", "true"), ("days", tomorrow.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        (HttpResponseMessage _, string closed) = await ada.GetAsync("/staff/support");
        Assert.DoesNotContain("Find the caller", closed, StringComparison.Ordinal);
    }

    // Owner, 2026-10-29 (ticket desk): a billing ticket goes to Finance only; the warehouse never sees it. Nobody takes it in time: the managers are told, once.
    [SqlServerFact]
    public async Task BillingTicket_IsSeenByFinanceOnly_AndEscalatedOnceWhenNobodyTakesIt()
    {
        Guid tenant = NewTenant("deptco", "Dept Co");
        using CookieClient owner = await OwnerAsync("deptco");
        Guid jean = await RegisteredCustomerAsync(tenant, "jean@example.com", "C00001", "+50937123456");
        string jeans = Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.Make("VX", new[] { 5, 5, 5, 1, 2, 3, 4, 5, 6, 7 });
        await ShipmentAsync(tenant, (jean, jeans));

        using CookieClient agent = await StaffAsync(owner, "deptco", "Calls", "ops.support.lookup");
        (HttpResponseMessage _, string start) = await agent.GetAsync("/staff/support");
        (HttpResponseMessage _, string caller) = await agent.PostAsync("/staff/support/caller", start, ("phone", "+50937123456"));
        string token = Regex.Match(caller, "name=\"caller\" value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        (HttpResponseMessage _, string found) = await agent.PostAsync("/staff/support/case", caller, ("caller", token), ("number", jeans));
        Assert.Equal("/staff/support?done=request", (await agent.PostAsync("/staff/support/request", found, ("caller", token), ("number", jeans), ("topic", "Billing"),
            ("text", "Disputes the amount"))).Response.Headers.Location?.OriginalString);

        using CookieClient warehouse = await PersonAsync(owner, "deptco", "wally", "Warehouse", "ops.parcels.view", "ops.parcels.assign");
        using CookieClient finance = await PersonAsync(owner, "deptco", "fiona", "Billing", "ops.invoices.view");

        (HttpResponseMessage _, string ownerDesk) = await owner.GetAsync("/staff/tickets?topic=Billing");
        string ticket = Regex.Match(ownerDesk, "/staff/tickets/(?<id>[0-9a-f-]{36})\"").Groups["id"].Value;
        Assert.NotEmpty(ticket);

        (HttpResponseMessage _, string wallyDesk) = await warehouse.GetAsync("/staff/tickets");
        Assert.DoesNotContain(ticket, wallyDesk, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await warehouse.GetAsync("/staff/tickets/" + ticket)).Response.StatusCode);

        (HttpResponseMessage _, string fionaDesk) = await finance.GetAsync("/staff/tickets");
        Assert.Contains(ticket, fionaDesk, StringComparison.Ordinal);
        (HttpResponseMessage _, string fionaPage) = await finance.GetAsync("/staff/tickets/" + ticket);
        Assert.Contains("Disputes the amount", fionaPage, StringComparison.Ordinal);
        Assert.Contains("I'm on it", WebUtility.HtmlDecode(fionaPage), StringComparison.Ordinal);

        // Five hours old, nobody took it: step 1, once; the second round sends nothing.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            Vixly.Operations.Infrastructure.Persistence.OperationsDbContext context = scope.ServiceProvider.GetRequiredService<Vixly.Operations.Infrastructure.Persistence.OperationsDbContext>();
            Guid id = Guid.Parse(ticket);
            await context.Tickets.Where(item => item.Id == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.CreatedUtc, DateTimeOffset.UtcNow.AddHours(-5)));
        }

        Vixly.Operations.Web.Hosting.OperationsTicketEscalationWorker escalations = _host.Services.GetRequiredService<Vixly.Operations.Web.Hosting.OperationsTicketEscalationWorker>();
        Assert.True(await escalations.RunAsync(CancellationToken.None) >= 1);
        Assert.Equal(1, await OutboxCountAsync(Vixly.Operations.Application.Tickets.OperationsTicketEscalatedNotice.MessageType, Guid.Parse(ticket)));
        await escalations.RunAsync(CancellationToken.None);
        Assert.Equal(1, await OutboxCountAsync(Vixly.Operations.Application.Tickets.OperationsTicketEscalatedNotice.MessageType, Guid.Parse(ticket)));
        (HttpResponseMessage _, string escalated) = await finance.GetAsync("/staff/tickets?status=escalated");
        Assert.Contains(ticket, escalated, StringComparison.Ordinal);
    }

    private async Task<int> OutboxCountAsync(string type, Guid stream)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
        return await scope.ServiceProvider.GetRequiredService<Vixly.Operations.Infrastructure.Persistence.OperationsDbContext>().OutboxMessages
            .CountAsync(item => item.Type == type && item.StreamId == stream);
    }

    // Another staff member with their own role (a role per person here, for the tests).
    private async Task<CookieClient> PersonAsync(CookieClient owner, string slug, string person, string roleName, params string[] permissions)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, new[] { ("name", roleName) }.Concat(permissions.Select(permission => ("permissions", permission))).ToArray());
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">" + Regex.Escape(roleName) + "</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", person + " Staff"), ("email", person + "@" + slug + ".example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));

        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(browser);
        return browser;
    }

    // Two booked shipments with their tracking numbers, at a Miami warehouse.
    private async Task ShipmentAsync(Guid tenant, params (Guid Customer, string Tracking)[] shipments)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        Vixly.Operations.Infrastructure.Persistence.OperationsDbContext context = scope.ServiceProvider.GetRequiredService<Vixly.Operations.Infrastructure.Persistence.OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Vixly.Operations.Domain.Locations.OperationsLocationDetails details = new("MIA", "Miami",
            Vixly.Operations.Domain.Locations.OperationsPostalAddress.Create("8400 NW 25th St", null, "Doral", "FL", "33122", "US"), null, null, null, true);
        Vixly.Operations.Domain.Locations.OperationsBranch branch = Vixly.Operations.Domain.Locations.OperationsBranch.Create(tenant, details, "US",
            Vixly.Operations.Domain.Locations.OperationsBranchLegal.None, now);
        Vixly.Operations.Domain.Locations.OperationsWarehouse warehouse = Vixly.Operations.Domain.Locations.OperationsWarehouse.Create(tenant, branch.Id, details,
            Vixly.Operations.Domain.Locations.OperationsWarehouseRoles.Receiving, true, now);
        context.AddRange(branch, warehouse);
        List<Vixly.Operations.Domain.Shipments.OperationsShipment> made = shipments
            .Select(item => Vixly.Operations.Domain.Shipments.OperationsShipment.Open(tenant, item.Customer, warehouse.Id, now)).ToList();
        context.AddRange(made);
        await context.SaveChangesAsync();

        for (int index = 0; index < made.Count; index++)
        {
            Guid id = made[index].Id;
            string tracking = shipments[index].Tracking;
            await context.Shipments.Where(item => item.Id == id).ExecuteUpdateAsync(set => set
                .SetProperty(item => item.Status, Vixly.Operations.Domain.Shipments.OperationsShipmentStatus.Booked)
                .SetProperty(item => item.TrackingNumber, tracking));
        }
    }

    // Review item 3: a confirmed customer email (at any company) is never a work email. The invitation goes as always (the company learns nothing);
    // only the mailbox's owner, accepting it, is told.
    [SqlServerFact]
    public async Task StaffInvite_WithACustomerEmailOfAnyCompany_IsRefusedWhenAccepted()
    {
        Guid other = NewTenant("custelse", "Custelse");
        await RegisteredCustomerAsync(other, "pat@custelse-buyer.example", "C00001");
        NewTenant("hireco", "Hire Co");
        using CookieClient owner = await OwnerAsync("hireco");

        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">[^<]+</option>").Groups["id"].Value;
        (HttpResponseMessage invited, string _) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Pat Buyer"), ("email", "pat@custelse-buyer.example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));
        Assert.Contains("?done=invited", invited.Headers.Location?.OriginalString, StringComparison.Ordinal);

        string link = _host.Mailer.Invitations.Last().Link;
        using CookieClient pat = _host.Browser("hireco.techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await pat.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string refused) = await pat.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));

        Assert.Null(accepted.Headers.Location);
        Assert.Contains("This email can't be used for a staff account.", System.Net.WebUtility.HtmlDecode(refused), StringComparison.Ordinal);
        Assert.DoesNotContain("Custelse", refused, StringComparison.Ordinal);
        Assert.Null(pat.Cookie(OperationsCookies.Session));
    }

    // A customer who signed up on the portal and confirmed the email.
    private async Task<Guid> RegisteredCustomerAsync(Guid tenant, string email, string code, string phone = "+50937123456")
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsCustomer customer = OperationsCustomer.SignUp(tenant, "Ana", "Pierre", null, EmailAddress.Create(email), phone, LanguageCode.En, "$2a$12$hash", DateTimeOffset.UtcNow);
        customer.ConfirmEmail(code, DateTimeOffset.UtcNow);
        scope.ServiceProvider.GetRequiredService<IOperationsCustomerRepository>().Add(customer);
        await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        return customer.Id;
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
    private Task<CookieClient> StaffAsync(CookieClient owner, string slug) => StaffAsync(owner, slug, "Website editor", "ops.brand.view", "ops.brand.edit");

    private async Task<CookieClient> StaffAsync(CookieClient owner, string slug, string roleName, params string[] permissions)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, new[] { ("name", roleName) }.Concat(permissions.Select(permission => ("permissions", permission))).ToArray());
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">" + Regex.Escape(roleName) + "</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", "Ada Lovelace"), ("email", "ada@" + slug + ".example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));

        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient ada = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await ada.GetAsync(new Uri(link).PathAndQuery);
        await ada.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(ada);
        return ada;
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
}
