using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using OtpNet;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsSignInHttpTests.cs
//
// PURPOSE:
// Proves the tenant landing and staff sign-in end to end (Slice 1A).
//
// RESPONSIBILITIES:
// - The landing shows the tenant name and the sign-in form, never "Vixly"
// - Owner invitation, password, authenticator enrolment, recovery codes, sign-out and sign-in with a recovery code
// - A session cookie does not work on another tenant host
//
// IMPORTANT NOTES:
// - Real pipeline, real database (VIXLY_TEST_SQL_SERVER); Platform and mail are stood in
// =========================================================
public sealed class OperationsSignInHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private static readonly Regex ManualKey = new("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant);

    private static readonly Regex RecoveryCode = new("<li><code>(?<code>[A-Z2-9]{5}-[A-Z2-9]{5})</code></li>", RegexOptions.CultureInvariant);

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsSignInHttpTests(OperationsWebDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync() => _host = await OperationsWebTestHost.StartAsync(_database.ConnectionString);

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [SqlServerFact]
    public async Task Owner_GoesFromTheEmail_ToTheSetupPage_AndBack()
    {
        Guid tenant = NewTenant("newco", "draft");
        using CookieClient browser = _host.Browser("newco.techcrafters.ltd");

        // The invitation email
        await _host.InviteOwnerAsync(tenant);
        string link = Assert.Single(_host.Mailer.Invitations).Link;
        Assert.StartsWith("https://newco.techcrafters.ltd/invitation?token=", link, StringComparison.Ordinal);

        // Choose a password
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        Assert.Contains("Newco Freight", invitation, StringComparison.Ordinal);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Token(link)), ("password", Password));
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        Assert.StartsWith("/staff/sign-in/enrol", accepted.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.Null(browser.Cookie(OperationsCookies.Session));

        // Enrol the authenticator app
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        string key = ManualKey.Match(enrol).Groups["key"].Value;
        Assert.NotEmpty(key);
        (HttpResponseMessage _, string codesPage) = await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(key)), ("returnUrl", "/setup"));
        string[] codes = RecoveryCode.Matches(codesPage).Select(match => match.Groups["code"].Value).ToArray();
        Assert.Equal(10, codes.Length);
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));

        // The setup page
        (HttpResponseMessage setup, string setupPage) = await browser.GetAsync("/setup");
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.Contains("Grace Hopper", setupPage, StringComparison.Ordinal);
        Assert.Contains("data-idle-warn=\"780\"", setupPage, StringComparison.Ordinal);

        // Sign out
        (HttpResponseMessage signedOut, string _) = await browser.PostAsync("/staff/sign-out", setupPage);
        Assert.Equal(HttpStatusCode.Redirect, signedOut.StatusCode);
        Assert.Null(browser.Cookie(OperationsCookies.Session));
        (HttpResponseMessage _, string again) = await browser.GetAsync("/setup");
        Assert.Contains("name=\"password\"", again, StringComparison.Ordinal);

        // Sign in with a recovery code (the app code of this step was used)
        (HttpResponseMessage password, string _) = await browser.PostAsync("/staff/sign-in", again, ("email", "grace@newco.example"), ("password", Password), ("returnUrl", "/setup"));
        Assert.StartsWith("/staff/sign-in/verify", password.Headers.Location!.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string recoveryPage) = await browser.GetAsync("/staff/sign-in/recovery?returnUrl=/setup");
        (HttpResponseMessage recovered, string _) = await browser.PostAsync("/staff/sign-in/recovery", recoveryPage, ("code", codes[0].ToLowerInvariant()), ("returnUrl", "/setup"));
        Assert.Equal("/setup", recovered.Headers.Location?.OriginalString);
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
    }

    [SqlServerFact]
    public async Task Landing_ShowsTheTenantName_AndTheSignInForm()
    {
        Guid tenant = NewTenant("northwind", "active", "Northwind Freight");
        await _host.InviteOwnerAsync(tenant);
        using CookieClient browser = _host.Browser("northwind.techcrafters.ltd");

        (HttpResponseMessage response, string html) = await browser.GetAsync("/staff");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Northwind Freight", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/staff/sign-in\"", html, StringComparison.Ordinal);
        Assert.Contains("vixly-forms.js", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Vixly", html.Replace("/_content/Vixly.Shared.UI/", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Landing_SpeaksTheTenantLanguage()
    {
        NewTenant("nordfr", "active", language: "fr");
        using CookieClient browser = _host.Browser("nordfr.techcrafters.ltd");

        (HttpResponseMessage _, string html) = await browser.GetAsync("/staff");

        Assert.Contains("lang=\"fr\"", html, StringComparison.Ordinal);
        Assert.Contains("Mot de passe", html, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task WrongPassword_SaysOnlyThatItFailed()
    {
        Guid tenant = NewTenant("acme", "active");
        await _host.InviteOwnerAsync(tenant);
        using CookieClient browser = _host.Browser("acme.techcrafters.ltd");
        (HttpResponseMessage _, string landing) = await browser.GetAsync("/staff");

        (HttpResponseMessage response, string html) = await browser.PostAsync("/staff/sign-in", landing, ("email", "grace@acme.example"), ("password", "not the right password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The email or password is not correct.", html, StringComparison.Ordinal);
        Assert.Null(browser.Cookie(OperationsCookies.SignIn));
    }

    [SqlServerFact]
    public async Task PostWithoutAntiforgery_IsRefused()
    {
        NewTenant("guarded", "active");

        HttpResponseMessage response = await _host.Client("guarded.techcrafters.ltd").PostAsync(
            "/staff/sign-in", new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = "a@b.example", ["password"] = "x" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SqlServerFact]
    public async Task LiveTenant_NeverShowsSetup()
    {
        NewTenant("livecorp", "active");

        HttpResponseMessage response = await _host.Client("livecorp.techcrafters.ltd").GetAsync("/setup");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [SqlServerFact]
    public async Task SessionCookie_DoesNotWorkOnAnotherTenantHost()
    {
        Guid tenant = NewTenant("first", "draft");
        NewTenant("second", "active");
        string session = await SignedInOwnerAsync(tenant, "first");

        HttpRequestMessage request = new(HttpMethod.Get, "/staff");
        request.Headers.Add("Cookie", OperationsCookies.Session + "=" + session);
        HttpResponseMessage response = await _host.Client("second.techcrafters.ltd").SendAsync(request);
        string html = await response.Content.ReadAsStringAsync();

        Assert.Contains("action=\"/staff/sign-in\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Grace Hopper", html, StringComparison.Ordinal);
    }

    private Guid NewTenant(string slug, string status, string? name = null, string language = "en")
    {
        Guid tenant = Guid.NewGuid();
        string company = name ?? char.ToUpperInvariant(slug[0]) + slug[1..] + " Freight";
        _host.Platform.Tenant(slug, tenant, status, language: language);
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, language, "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    private async Task<string> SignedInOwnerAsync(Guid tenant, string slug)
    {
        await _host.InviteOwnerAsync(tenant);
        using CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last().Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Token(link)), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(ManualKey.Match(enrol).Groups["key"].Value)));
        return browser.Cookie(OperationsCookies.Session) ?? throw new InvalidOperationException("No session.");
    }

    private static string Token(string link) => Uri.UnescapeDataString(link.Split("token=")[1]);

    private static string Code(string manualKey) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(DateTime.UtcNow);
}
