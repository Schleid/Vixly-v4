using System.Net;
using System.Text.RegularExpressions;
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

        // The menu: billing and Terms are tabs of Company, not menu items of their own (owner, 2026-10-18).
        Assert.Contains("Team and access", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/staff/billing\"", empty, StringComparison.Ordinal);
        (HttpResponseMessage _, string company) = await owner.GetAsync("/staff/company");
        Assert.Contains("href=\"/staff/billing\"", company, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff/legal\"", company, StringComparison.Ordinal);

        Guid customerId = await RegisteredCustomerAsync(tenant, "ana@example.com", "C00001");
        string customerPath = "/staff/customers/" + customerId;

        (HttpResponseMessage _, string found) = await owner.GetAsync("/staff/customers?search=C00001");
        Assert.Contains("Ana Pierre", found, StringComparison.Ordinal);
        (HttpResponseMessage _, string page) = await owner.GetAsync(customerPath);
        Assert.Contains("+50937123456", page, StringComparison.Ordinal);
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
        (HttpResponseMessage credit, string _) = await owner.PostAsync(customerPath + "/credit", page, ("allowed", "true"), ("reason", "Good customer since 2019"));
        Assert.Equal(customerPath + "?done=credit", credit.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string after) = await owner.GetAsync(customerPath);
        Assert.Contains("Ana Pierre", after, StringComparison.Ordinal);
        Assert.Contains("Suspended", after, StringComparison.Ordinal);
        Assert.Contains("Unpaid shipments", after, StringComparison.Ordinal);
        Assert.Contains("Credit allowed", after, StringComparison.Ordinal);

        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=customers");
        Assert.Contains("ops.customers.status_changed", history, StringComparison.Ordinal);
        Assert.Contains("ops.customers.credit_changed", history, StringComparison.Ordinal);
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

    // A customer who signed up on the portal and confirmed the email.
    private async Task<Guid> RegisteredCustomerAsync(Guid tenant, string email, string code)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsCustomer customer = OperationsCustomer.SignUp(tenant, "Ana", "Pierre", null, EmailAddress.Create(email), "+50937123456", LanguageCode.En, "$2a$12$hash", DateTimeOffset.UtcNow);
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
