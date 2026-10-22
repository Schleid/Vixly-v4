using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using OtpNet;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsStaffHttpTests.cs
//
// PURPOSE:
// Proves staff, roles and step-up end to end (Slice 1D).
//
// RESPONSIBILITIES:
// - The Owner makes a role, invites a person with it, and the role decides what the person can do
// - A suspended person is signed out
// - A sensitive action without a fresh code goes to the confirm page and back
// - Ownership goes to an Administrator who has an authenticator app
//
// IMPORTANT NOTES:
// - Real pipeline, real database (VIXLY_TEST_SQL_SERVER); Platform and mail are stood in
// =========================================================
public sealed class OperationsStaffHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private static readonly Regex ManualKey = new("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant);

    private static readonly Regex RecoveryCode = new("<li><code>(?<code>[A-Z2-9]{5}-[A-Z2-9]{5})</code></li>", RegexOptions.CultureInvariant);

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsStaffHttpTests(OperationsWebDatabase database)
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
    public async Task Owner_InvitesAPerson_AndTheirRoleDecidesWhatTheyCanDo()
    {
        Guid tenant = NewTenant("staffco", "Staff Co");
        (CookieClient browser, string _) = await OwnerAsync(tenant, "staffco");
        using CookieClient owner = browser;

        // A role that edits the website only
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        Assert.DoesNotContain("value=\"ops.devices.approve\"", form, StringComparison.Ordinal);
        (HttpResponseMessage saved, string _) = await owner.PostAsync("/staff/roles/save", form, ("name", "Website editor"), ("permissions", "ops.brand.view"), ("permissions", "ops.brand.edit"));
        Assert.Equal("/staff/roles?done=saved", saved.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string roles) = await owner.GetAsync("/staff/roles");
        Assert.Contains("Website editor", roles, StringComparison.Ordinal);
        Assert.Contains("Administrator", roles, StringComparison.Ordinal);

        // Invite a person with it
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Website editor</option>").Groups["id"].Value;
        Assert.NotEmpty(role);
        (HttpResponseMessage invited, string _) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Ada Lovelace"), ("email", "ada@staffco.example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));
        Assert.Contains("?done=invited", invited.Headers.Location?.OriginalString, StringComparison.Ordinal);
        string member = invited.Headers.Location!.OriginalString.Split('?')[0];
        string link = _host.Mailer.Invitations.Last().Link;
        Assert.Equal("ada@staffco.example", _host.Mailer.Invitations.Last().ToAddress);
        Assert.DoesNotContain("Vixly", _host.Mailer.Invitations.Last().CompanyName, StringComparison.Ordinal);

        // The same email cannot be invited twice
        (HttpResponseMessage _, string twice) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Ada Again"), ("email", "ADA@staffco.example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));
        Assert.Contains("Someone on the staff already uses this email.", twice, StringComparison.Ordinal);

        // Ada accepts; a role without device or staff rights needs no authenticator app
        using CookieClient ada = _host.Browser("staffco.techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await ada.GetAsync(new Uri(link).PathAndQuery);
        await ada.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        Assert.NotNull(ada.Cookie(OperationsCookies.Session));
        await EnrolAsync(ada);

        Assert.Equal(HttpStatusCode.OK, (await ada.GetAsync("/staff/site")).Response.StatusCode);
        (HttpResponseMessage _, string refused) = await ada.GetAsync("/staff/people");
        Assert.Contains("Your role does not allow this.", refused, StringComparison.Ordinal);
        Assert.DoesNotContain("Grace Hopper", refused, StringComparison.Ordinal);
        (HttpResponseMessage _, string noRoles) = await ada.GetAsync("/staff/roles");
        Assert.Contains("Your role does not allow this.", noRoles, StringComparison.Ordinal);
        (HttpResponseMessage _, string noDevices) = await ada.GetAsync("/staff/devices");
        Assert.Contains("Your role does not allow this.", noDevices, StringComparison.Ordinal);

        // The Owner sees Ada with her role, then suspends her: she is signed out
        (HttpResponseMessage _, string page) = await owner.GetAsync(member);
        Assert.Contains("Ada Lovelace", page, StringComparison.Ordinal);
        Assert.Contains("Website editor", page, StringComparison.Ordinal);
        (HttpResponseMessage _, string noReason) = await owner.PostAsync(member + "/status", page, ("status", "Suspended"), ("reason", " "));
        Assert.Contains("Enter a reason.", noReason, StringComparison.Ordinal);
        (HttpResponseMessage suspended, string _) = await owner.PostAsync(member + "/status", page, ("status", "Suspended"), ("reason", "Investigation"));
        Assert.Equal(member + "?done=status", suspended.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string after) = await ada.GetAsync("/staff");
        Assert.Contains("action=\"/staff/sign-in\"", after, StringComparison.Ordinal);
        (HttpResponseMessage _, string landing) = await ada.GetAsync("/staff");
        (HttpResponseMessage _, string again) = await ada.PostAsync("/staff/sign-in", landing, ("email", "ada@staffco.example"), ("password", Password));
        Assert.Null(ada.Cookie(OperationsCookies.Session));
        Assert.Contains("action=\"/staff/sign-in\"", again, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task SensitiveAction_AsksForAFreshCode_ThenGoesBack()
    {
        Guid tenant = NewTenant("stepco", "Step Co");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "stepco");
        using CookieClient owner = browser;
        await AgeSecondFactorAsync(tenant);

        (HttpResponseMessage _, string account) = await owner.GetAsync("/staff/account");
        Assert.Contains("10 recovery codes left", account, StringComparison.Ordinal);
        (HttpResponseMessage asked, string _) = await owner.PostAsync("/staff/account/recovery-codes", account);
        Assert.Equal("/staff/confirm?returnUrl=%2Fstaff%2Faccount", asked.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string confirm) = await owner.GetAsync(asked.Headers.Location!.OriginalString);
        (HttpResponseMessage _, string wrong) = await owner.PostAsync("/staff/confirm", confirm, ("code", "000000"), ("returnUrl", "/staff/account"));
        Assert.DoesNotContain("10 recovery codes left", wrong, StringComparison.Ordinal);
        (HttpResponseMessage confirmed, string _) = await owner.PostAsync("/staff/confirm", confirm, ("code", Code(key, DateTime.UtcNow.AddSeconds(30))), ("returnUrl", "/staff/account"));
        Assert.Equal("/staff/account", confirmed.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string fresh) = await owner.GetAsync("/staff/account");
        (HttpResponseMessage _, string codes) = await owner.PostAsync("/staff/account/recovery-codes", fresh);
        Assert.Equal(10, RecoveryCode.Matches(codes).Count);
        Assert.Contains("shown only once", codes, StringComparison.Ordinal);
    }

    // Owner, 2026-10-21: the type and browser of a device; each sign-in with its device and country; another country than the device's is flagged.
    [SqlServerFact]
    public async Task Device_TypeAndBrowser_ThenASignInFromAnotherCountry_IsFlaggedInTheActivity()
    {
        Guid tenant = NewTenant("travelco", "Travel Co");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "travelco");
        using CookieClient owner = browser;
        owner.Header("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0 Safari/537.36");
        owner.Header("CF-IPCountry", "US");

        (HttpResponseMessage _, string enrolPage) = await owner.GetAsync("/devices/enrol");
        Assert.Contains("value=\"laptop\"", enrolPage, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"scanner\"", enrolPage, StringComparison.Ordinal);
        await owner.PostAsync("/devices/enrol", enrolPage, ("name", "Front desk laptop"), ("kind", "laptop"));
        (HttpResponseMessage _, string devices) = await owner.GetAsync("/staff/devices");
        Assert.Contains("Front desk laptop", devices, StringComparison.Ordinal);
        Assert.Contains("of 5 licences in use", devices, StringComparison.Ordinal);
        Assert.Contains("<td>D-0000", devices, StringComparison.Ordinal);
        Assert.Contains(">Laptop<", devices, StringComparison.Ordinal);
        Assert.Contains("<td>Chrome</td>", devices, StringComparison.Ordinal);
        Assert.Contains("<td>Grace Hopper, ", devices, StringComparison.Ordinal);
        Assert.Contains(">Free<", devices, StringComparison.Ordinal);

        // Signed out, then in again from another country: the code is asked (as always for the Owner), and the sign-in is flagged.
        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        await owner.PostAsync("/staff/sign-out", home);
        owner.Header("CF-IPCountry", "FR");
        (HttpResponseMessage _, string signIn) = await owner.GetAsync("/staff");
        (HttpResponseMessage password, string _) = await owner.PostAsync("/staff/sign-in", signIn, ("email", "grace@travelco.example"), ("password", Password));
        Assert.StartsWith("/staff/sign-in/verify", password.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string verify) = await owner.GetAsync(password.Headers.Location!.OriginalString);
        await owner.PostAsync("/staff/sign-in/verify", verify, ("code", Code(key, DateTime.UtcNow.AddSeconds(30))));

        (HttpResponseMessage _, string activity) = await owner.GetAsync("/staff/activity");
        Assert.Contains("Front desk laptop", activity, StringComparison.Ordinal);
        Assert.Contains("data-flag=\"country\"", activity, StringComparison.Ordinal);
        Assert.Contains("FR", activity, StringComparison.Ordinal);
        Assert.Contains("signed out", activity, StringComparison.Ordinal);
        Assert.Contains("Still signed in", activity, StringComparison.Ordinal);
        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=auth");
        Assert.Contains("ops.auth.country_changed", history, StringComparison.Ordinal);

        // Confirmed in France, not elsewhere: used from a third country, the session ends and the sign-in page says why.
        owner.Header("CF-IPCountry", "DE");
        (HttpResponseMessage moved, string _) = await owner.GetAsync("/staff/devices");
        Assert.Equal("/staff?country=1", moved.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string why) = await owner.GetAsync("/staff?country=1");
        Assert.Contains("used from another country", why, StringComparison.Ordinal);
        Assert.NotNull(owner.Cookie(OperationsCookies.Device));
    }

    // Owner, 2026-10-21: signed in without the device (no flag), then the device's cookie from another country: the session ends.
    [SqlServerFact]
    public async Task SignIn_WithoutTheDevice_ThenItsCookieFromAnotherCountry_EndsTheSession()
    {
        Guid tenant = NewTenant("sneakco", "Sneak Co");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "sneakco");
        using CookieClient owner = browser;
        owner.Header("CF-IPCountry", "US");
        (HttpResponseMessage _, string enrolPage) = await owner.GetAsync("/devices/enrol");
        await owner.PostAsync("/devices/enrol", enrolPage, ("name", "Desk"), ("kind", "desktop"));
        string deviceCookie = owner.Cookie(OperationsCookies.Device)!;

        // No country header at all (not through Cloudflare) tells nothing: the session goes on.
        owner.Header("CF-IPCountry", null);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/staff/devices")).Response.StatusCode);

        using CookieClient abroad = _host.Browser("sneakco.techcrafters.ltd");
        abroad.Header("CF-IPCountry", "FR");
        (HttpResponseMessage _, string signIn) = await abroad.GetAsync("/staff");
        (HttpResponseMessage password, string _) = await abroad.PostAsync("/staff/sign-in", signIn, ("email", "grace@sneakco.example"), ("password", Password));
        (HttpResponseMessage _, string verify) = await abroad.GetAsync(password.Headers.Location!.OriginalString);
        await abroad.PostAsync("/staff/sign-in/verify", verify, ("code", Code(key, DateTime.UtcNow.AddSeconds(30))));
        Assert.NotNull(abroad.Cookie(OperationsCookies.Session));

        abroad.Cookies.Add(new Uri("https://sneakco.techcrafters.ltd/"), new System.Net.Cookie(OperationsCookies.Device, deviceCookie) { Secure = true, Path = "/" });
        (HttpResponseMessage ended, string _) = await abroad.GetAsync("/staff/devices");

        Assert.Equal("/staff?country=1", ended.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string activity) = await owner.GetAsync("/staff/activity");
        Assert.Contains("used from another country", activity, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task PostWithoutTheFormToken_IsRefused_EvenWithNoOtherField()
    {
        Guid tenant = NewTenant("tokenco", "Token Co");
        (CookieClient browser, string _) = await OwnerAsync(tenant, "tokenco");
        using CookieClient owner = browser;

        (HttpResponseMessage refused, string _) = await owner.PostAsync("/staff/account/recovery-codes", string.Empty);
        (HttpResponseMessage pay, string _) = await owner.PostAsync($"/staff/billing/invoices/{FakeCompanyClient.InvoiceId}/pay", string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pay.StatusCode);
    }

    [SqlServerFact]
    public async Task ConfirmPage_NeverSendsAnywhereElse()
    {
        Guid tenant = NewTenant("redirco", "Redir Co");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "redirco");
        using CookieClient owner = browser;

        (HttpResponseMessage _, string confirm) = await owner.GetAsync("/staff/confirm?returnUrl=https://evil.example/");
        Assert.Contains("value=\"/staff\"", confirm, StringComparison.Ordinal);
        (HttpResponseMessage confirmed, string _) = await owner.PostAsync("/staff/confirm", confirm, ("code", Code(key, DateTime.UtcNow.AddSeconds(30))), ("returnUrl", "//evil.example/"));
        Assert.Equal("/staff", confirmed.Headers.Location?.OriginalString);
    }

    [SqlServerFact]
    public async Task Ownership_GoesToAnAdministratorWithAnAuthenticatorApp()
    {
        Guid tenant = NewTenant("handco", "Hand Co");
        (CookieClient browser, string _) = await OwnerAsync(tenant, "handco");
        using CookieClient owner = browser;

        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string administrator = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Administrator</option>").Groups["id"].Value;
        (HttpResponseMessage invited, string _) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Katherine Johnson"), ("email", "katherine@handco.example"), ("language", "en"), ("roleId", administrator), ("scope", "tenant"));
        string member = invited.Headers.Location!.OriginalString.Split('?')[0];

        // No authenticator app yet: no transfer offered, and a forced post is refused
        (HttpResponseMessage _, string page) = await owner.GetAsync(member);
        Assert.DoesNotContain("/transfer", page, StringComparison.Ordinal);
        (HttpResponseMessage _, string refused) = await owner.PostAsync(member + "/transfer", page, ("confirm", "true"));
        Assert.Contains("Ownership can go only to an active Administrator", refused, StringComparison.Ordinal);

        // An Administrator must enrol an authenticator app at the first sign-in
        string link = _host.Mailer.Invitations.Last().Link;
        using CookieClient katherine = _host.Browser("handco.techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await katherine.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await katherine.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        Assert.StartsWith("/staff/sign-in/enrol", accepted.Headers.Location!.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string enrol) = await katherine.GetAsync(accepted.Headers.Location!.OriginalString);
        await katherine.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(ManualKey.Match(enrol).Groups["key"].Value, DateTime.UtcNow)));
        Assert.NotNull(katherine.Cookie(OperationsCookies.Session));

        (HttpResponseMessage _, string ready) = await owner.GetAsync(member);
        Assert.Contains(member + "/transfer", ready, StringComparison.Ordinal);
        (HttpResponseMessage moved, string _) = await owner.PostAsync(member + "/transfer", ready, ("confirm", "true"));
        Assert.Equal(member + "?done=transferred", moved.Headers.Location?.OriginalString);

        // The former Owner is now an Administrator and cannot change the new Owner
        (HttpResponseMessage _, string after) = await owner.GetAsync(member);
        Assert.Contains("<strong>Owner</strong>", after, StringComparison.Ordinal);
        Assert.DoesNotContain(member + "/status", after, StringComparison.Ordinal);
        (HttpResponseMessage _, string list) = await owner.GetAsync("/staff/people");
        Assert.Contains("Katherine Johnson", list, StringComparison.Ordinal);
    }

    private static string Code(string manualKey, DateTime at) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(at);

    private Guid NewTenant(string slug, string company)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, "active");
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, "en", "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    // The sign-in code is older than the step-up window (5 minutes).
    private async Task AgeSecondFactorAsync(Guid tenant)
    {
        await using SqlConnection connection = new(_database.ConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("UPDATE ops.UserSessions SET SecondFactorUtc = DATEADD(MINUTE, -10, SecondFactorUtc) WHERE TenantId = @tenant", connection);
        command.Parameters.AddWithValue("@tenant", tenant);
        Assert.True(await command.ExecuteNonQueryAsync() > 0);
    }

    private static async Task EnrolAsync(CookieClient browser)
    {
        (HttpResponseMessage _, string page) = await browser.GetAsync("/devices/enrol");
        await browser.PostAsync("/devices/enrol", page, ("name", "Laptop"), ("kind", "pc"));
        Assert.NotNull(browser.Cookie(OperationsCookies.Device));
    }

    // The Owner accepts the invitation, enrols an authenticator app and a computer.
    private async Task<(CookieClient Browser, string Key)> OwnerAsync(Guid tenant, string slug)
    {
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        string key = ManualKey.Match(enrol).Groups["key"].Value;
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(key, DateTime.UtcNow)));
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
        await EnrolAsync(browser);
        return (browser, key);
    }
}
