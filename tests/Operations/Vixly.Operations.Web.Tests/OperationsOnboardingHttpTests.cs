using System.Net;
using System.Text.RegularExpressions;
using OtpNet;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsOnboardingHttpTests.cs
//
// PURPOSE:
// Proves the onboarding completion of Slice 1C end to end.
//
// RESPONSIBILITIES:
// - The Owner asks for the first-device link during setup, completes it on this computer, and the tenant goes live
// - Staff of a live tenant work only from a licensed device: enrolment, waiting, approval with a fresh code, release
// - Device secrets rotate into the cookie
// - "Forgot your password?" emails a single-use link; the new password ends every session
//
// IMPORTANT NOTES:
// - Real pipeline, real database (VIXLY_TEST_SQL_SERVER); Platform, devices and mail are stood in
// =========================================================
public sealed class OperationsOnboardingHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private static readonly Regex ManualKey = new("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant);

    private static readonly Regex RecoveryCode = new("<li><code>(?<code>[A-Z2-9]{5}-[A-Z2-9]{5})</code></li>", RegexOptions.CultureInvariant);

    private readonly OperationsWebDatabase _database;

    private List<string> _recoveryCodes = new();

    private OperationsWebTestHost _host = null!;

    public OperationsOnboardingHttpTests(OperationsWebDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync() => _host = await OperationsWebTestHost.StartAsync(_database.ConnectionString);

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [SqlServerFact]
    public async Task Owner_EnrolsTheFirstComputer_AndTheTenantGoesLive()
    {
        Guid tenant = NewTenant("firstco", "First Co", "pending_activation");
        _host.Devices.OnFirstDevice = id => _host.Platform.Tenant("firstco", id, "active");
        (CookieClient browser, _) = await OwnerAsync(tenant, "firstco");
        using CookieClient owner = browser;

        // Setup: password and authenticator are done; the last step is this computer.
        (HttpResponseMessage _, string setup) = await owner.GetAsync("/setup");
        Assert.Contains("Email me the link for this computer", setup, StringComparison.Ordinal);
        Assert.DoesNotContain("Terms", setup, StringComparison.Ordinal);
        (HttpResponseMessage asked, string _) = await owner.PostAsync("/setup/first-device", setup);
        Assert.Equal("/setup?link=sent", asked.Headers.Location?.OriginalString);
        (Guid linkTenant, string token) = Assert.Single(_host.Devices.FirstDeviceLinks);
        Assert.Equal(tenant, linkTenant);
        Assert.Equal("owner", _host.Devices.Actors.Last().Role);

        // The emailed link opens on the tenant host, which is still being set up.
        (HttpResponseMessage linkPage, string linkHtml) = await owner.GetAsync("/devices/first");
        Assert.Equal(HttpStatusCode.OK, linkPage.StatusCode);
        Assert.Contains("data-device-token", linkHtml, StringComparison.Ordinal);
        Assert.Contains("/js/ops-device-link.js", linkHtml, StringComparison.Ordinal);

        (HttpResponseMessage done, string _) = await owner.PostAsync("/devices/link", linkHtml, ("token", token), ("name", "Front desk PC"), ("kind", "pc"));
        Assert.Equal("/staff?device=ready", done.Headers.Location?.OriginalString);
        Assert.NotNull(owner.Cookie(OperationsCookies.Device));

        // Live now: the workspace opens from this licensed computer.
        (HttpResponseMessage workspace, string workspaceHtml) = await owner.GetAsync("/staff");
        Assert.Equal(HttpStatusCode.OK, workspaceHtml.Length > 0 ? workspace.StatusCode : HttpStatusCode.NoContent);
        Assert.Contains("Grace Hopper", workspaceHtml, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/staff/site")).Response.StatusCode);

        // A link works once.
        (HttpResponseMessage _, string again) = await owner.PostAsync("/devices/link", linkHtml, ("token", token), ("name", "Second"), ("kind", "pc"));
        Assert.Contains("no longer valid", again, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task NewComputer_WaitsForApproval_ThenWorks_UntilItIsStopped()
    {
        _host.Devices.AutoApprove = true;
        Guid tenant = NewTenant("deviceco", "Device Co", "active");
        (CookieClient approver, string key) = await OwnerAsync(tenant, "deviceco");
        using CookieClient owner = approver;
        await EnrolAsync(owner);

        // The same Owner on a second computer: signed in, but the computer is not licensed.
        _host.Devices.AutoApprove = false;
        using CookieClient laptop = _host.Browser("deviceco.techcrafters.ltd");
        await SignInWithRecoveryCodeAsync(laptop, _recoveryCodes[0]);
        (HttpResponseMessage gated, string _) = await laptop.GetAsync("/staff/site");
        Assert.Equal("/devices/enrol", gated.Headers.Location?.OriginalString);
        await EnrolAsync(laptop);
        Assert.Equal("/devices/waiting", (await laptop.GetAsync("/staff")).Response.Headers.Location?.OriginalString);

        // Approval needs a fresh authenticator code.
        // The Licences tab lists the request; its page approves it (owner, 2026-10-21).
        (HttpResponseMessage _, string licences) = await owner.GetAsync("/staff/devices");
        Assert.Contains("Requested", licences, StringComparison.Ordinal);
        string pending = Regex.Match(licences, "data-status=\"pending\">[\\s\\S]*?/staff/devices/(?<id>[0-9a-f-]{36})\"").Groups["id"].Value;
        (HttpResponseMessage _, string devices) = await owner.GetAsync("/staff/devices/" + pending);
        Assert.Contains("/staff/devices/" + pending + "/decide", devices, StringComparison.Ordinal);
        (HttpResponseMessage _, string noCode) = await owner.PostAsync($"/staff/devices/{pending}/decide", devices, ("approve", "true"), ("code", "000000"));
        Assert.Contains("That code did not work. Try again.", noCode, StringComparison.Ordinal);
        (HttpResponseMessage approved, string _) = await owner.PostAsync($"/staff/devices/{pending}/decide", devices, ("approve", "true"), ("code", Code(key, DateTime.UtcNow.AddSeconds(30))));
        Assert.Equal("/staff/devices?done=1", approved.Headers.Location?.OriginalString);
        Assert.True(_host.Devices.Actors.Last().StepUp);

        // The approved computer works; a rotated secret replaces the cookie.
        string before = laptop.Cookie(OperationsCookies.Device)!;
        _host.Devices.NextRotation = "rotated-secret";
        Assert.Equal(HttpStatusCode.OK, (await laptop.GetAsync("/staff/site")).Response.StatusCode);
        Assert.NotEqual(before, laptop.Cookie(OperationsCookies.Device));
        Assert.EndsWith(".rotated-secret", laptop.Cookie(OperationsCookies.Device), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await laptop.GetAsync("/staff/locations")).Response.StatusCode);
    }

    [SqlServerFact]
    public async Task StoppedComputer_IsSentBackToEnrolment()
    {
        Guid tenant = NewTenant("stopco", "Stop Co", "active");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "stopco");
        using CookieClient owner = browser;
        await EnrolAsync(owner);

        (HttpResponseMessage _, string licences) = await owner.GetAsync("/staff/devices");
        string active = Regex.Match(licences, "data-status=\"active\">[\\s\\S]*?/staff/devices/(?<id>[0-9a-f-]{36})\"").Groups["id"].Value;
        (HttpResponseMessage _, string devices) = await owner.GetAsync("/staff/devices/" + active);
        Assert.Contains("/staff/devices/" + active + "/release", devices, StringComparison.Ordinal);
        (HttpResponseMessage released, string _) = await owner.PostAsync($"/staff/devices/{active}/release", devices, ("reason", "lost_or_stolen"), ("code", Code(key, DateTime.UtcNow.AddSeconds(30))));
        Assert.Equal("/staff/devices?done=1", released.Headers.Location?.OriginalString);
        Assert.Equal("released", _host.Devices.StatusOf(Guid.Parse(active)));

        // The positive check is kept at most 5 minutes; a refusal never. Here the next check refuses.
        _host.ClearDeviceChecks();
        (HttpResponseMessage gated, string _) = await owner.GetAsync("/staff/site");
        Assert.Equal("/devices/enrol?reason=device_released", gated.Headers.Location?.OriginalString);
        Assert.Null(owner.Cookie(OperationsCookies.Device));
        (HttpResponseMessage _, string enrolPage) = await owner.GetAsync("/devices/enrol?reason=device_released");
        Assert.Contains("This computer was released", enrolPage, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task ForgotPassword_EmailsASingleUseLink_AndTheNewPasswordEndsEverySession()
    {
        Guid tenant = NewTenant("resetco", "Reset Co", "active");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "resetco");
        using CookieClient owner = browser;
        await EnrolAsync(owner);
        using CookieClient visitor = _host.Browser("resetco.techcrafters.ltd");

        (HttpResponseMessage _, string signIn) = await visitor.GetAsync("/staff");
        Assert.Contains("href=\"/staff/forgot-password\"", signIn, StringComparison.Ordinal);

        // The same answer for an unknown address and a real one; only the real one gets an email.
        (HttpResponseMessage _, string forgot) = await visitor.GetAsync("/staff/forgot-password");
        (HttpResponseMessage _, string unknown) = await visitor.PostAsync("/staff/forgot-password", forgot, ("email", "nobody@resetco.example"));
        (HttpResponseMessage _, string known) = await visitor.PostAsync("/staff/forgot-password", forgot, ("email", "grace@resetco.example"));
        Assert.Contains("If an account uses this address", unknown, StringComparison.Ordinal);
        Assert.Contains("If an account uses this address", known, StringComparison.Ordinal);
        string link = Assert.Single(_host.Mailer.Resets).Link;
        Assert.StartsWith("https://resetco.techcrafters.ltd/staff/reset-password?token=", link, StringComparison.Ordinal);

        (HttpResponseMessage resetPage, string resetHtml) = await visitor.GetAsync(new Uri(link).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, resetPage.StatusCode);
        Assert.Contains("<meta name=\"referrer\" content=\"no-referrer\"", resetHtml, StringComparison.Ordinal);
        string token = Uri.UnescapeDataString(link.Split("token=")[1]);
        (HttpResponseMessage _, string weak) = await visitor.PostAsync("/staff/reset-password", resetHtml, ("token", token), ("password", "short"));
        Assert.Contains("at least", weak, StringComparison.Ordinal);
        (HttpResponseMessage reset, string _) = await visitor.PostAsync("/staff/reset-password", resetHtml, ("token", token), ("password", "a brand new long password"));
        Assert.Equal("/staff?reset=done", reset.Headers.Location?.OriginalString);

        // Every old session ended; the link cannot be used again; the new password signs in, and the code is still asked.
        Assert.Equal("/staff?ended=1", (await owner.GetAsync("/staff/site")).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string reused) = await visitor.PostAsync("/staff/reset-password", resetHtml, ("token", token), ("password", "another long password here"));
        Assert.Contains("no longer valid", reused, StringComparison.Ordinal);
        (HttpResponseMessage _, string page) = await visitor.GetAsync("/staff");
        (HttpResponseMessage signedIn, string _) = await visitor.PostAsync("/staff/sign-in", page, ("email", "grace@resetco.example"), ("password", "a brand new long password"));
        Assert.StartsWith("/staff/sign-in/verify", signedIn.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.NotNull(key);
    }

    [SqlServerFact]
    public async Task OverdueTerms_MakeTheCompanyReadOnly_UntilTheOwnerAccepts()
    {
        Guid tenant = NewTenant("legalco", "Legal Co", "active");
        (CookieClient browser, _) = await OwnerAsync(tenant, "legalco");
        using CookieClient owner = browser;
        await EnrolAsync(owner);
        _host.Platform.Tenant("legalco", tenant, "active", access: "read_only", legal: "overdue");
        _host.Legal.Status = FakeLegalClient.Overdue();
        _host.Legal.OnAccepted = id => _host.Platform.Tenant("legalco", id, "active");
        await _host.ForgetTenantAsync(tenant);

        // Reading works and says why; a change goes to the "read-only" page.
        (HttpResponseMessage _, string site) = await owner.GetAsync("/staff/site");
        Assert.Contains("read-only until the owner accepts", site, StringComparison.Ordinal);
        (HttpResponseMessage refused, string _) = await owner.PostAsync("/staff/site", site, ("displayName", "Legal Co"));
        Assert.Equal(HttpStatusCode.SeeOther, refused.StatusCode);
        Assert.Equal("/staff/limited", refused.Headers.Location?.OriginalString);

        // The Terms page: version, summary, the PDF from the tenant host, and the acceptance.
        Assert.Equal("/staff/legal", (await _host.Client("legalco.techcrafters.ltd").GetAsync("/legal")).Headers.Location?.OriginalString);
        (HttpResponseMessage _, string legal) = await owner.GetAsync("/staff/legal");
        Assert.Contains("2026-12", legal, StringComparison.Ordinal);
        Assert.Contains("Shorter notice periods.", legal, StringComparison.Ordinal);
        (HttpResponseMessage pdf, string _) = await owner.GetAsync($"/staff/legal/documents/{FakeLegalClient.TermsId}/en");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        (HttpResponseMessage accepted, string _) = await owner.PostAsync("/staff/legal/accept", legal,
            ("documentId", FakeLegalClient.TermsId.ToString()), ("sha256", new string('f', 64)), ("language", "en"), ("agree", "true"));
        Assert.Equal("/staff/legal?done=1", accepted.Headers.Location?.OriginalString);
        Assert.Equal("owner", Assert.Single(_host.Legal.Acceptances).Actor.Role);

        // Read-only ends at once.
        (HttpResponseMessage _, string after) = await owner.GetAsync("/staff/site");
        Assert.DoesNotContain("read-only", after, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task SupportSecondFactorReset_NeedsThePassword_AndEndsInANewEnrolment()
    {
        Guid tenant = NewTenant("lostco", "Lost Co", "active");
        (CookieClient browser, _) = await OwnerAsync(tenant, "lostco");
        using CookieClient owner = browser;
        await EnrolAsync(owner);
        _host.Devices.SecondFactorLinks["reset-token"] = tenant;
        using CookieClient phone = _host.Browser("lostco.techcrafters.ltd");

        (HttpResponseMessage page, string html) = await phone.GetAsync("/account/second-factor-reset");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("data-device-token", html, StringComparison.Ordinal);

        // A wrong password spends nothing.
        (HttpResponseMessage _, string wrong) = await phone.PostAsync("/account/second-factor-reset", html, ("token", "reset-token"), ("email", "grace@lostco.example"), ("password", "not my password"));
        Assert.True(_host.Devices.SecondFactorLinks.ContainsKey("reset-token"));
        Assert.DoesNotContain("data-idle-warn", wrong, StringComparison.Ordinal);

        (HttpResponseMessage done, string _) = await phone.PostAsync("/account/second-factor-reset", html, ("token", "reset-token"), ("email", "grace@lostco.example"), ("password", Password));
        Assert.StartsWith("/staff/sign-in/enrol", done.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.False(_host.Devices.SecondFactorLinks.ContainsKey("reset-token"));

        // Every old session ended; the Owner enrols a new authenticator app.
        Assert.Equal("/staff?ended=1", (await owner.GetAsync("/staff/site")).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string enrol) = await phone.GetAsync(done.Headers.Location!.OriginalString);
        string newKey = ManualKey.Match(enrol).Groups["key"].Value;
        Assert.NotEmpty(newKey);
        (HttpResponseMessage _, string codes) = await phone.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(newKey, DateTime.UtcNow)));
        Assert.Equal(10, RecoveryCode.Matches(codes).Count);

        // The link works once (the page is opened again: the form is tied to who is signed in).
        (HttpResponseMessage _, string fresh) = await phone.GetAsync("/account/second-factor-reset");
        (HttpResponseMessage _, string again) = await phone.PostAsync("/account/second-factor-reset", fresh, ("token", "reset-token"), ("email", "grace@lostco.example"), ("password", Password));
        Assert.Contains("no longer valid", again, StringComparison.Ordinal);
    }

    private static string Code(string manualKey, DateTime at) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(at);

    private Guid NewTenant(string slug, string company, string status)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, status);
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, "en", "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    private static async Task EnrolAsync(CookieClient browser)
    {
        (HttpResponseMessage _, string page) = await browser.GetAsync("/devices/enrol");
        await browser.PostAsync("/devices/enrol", page, ("name", "Laptop"), ("kind", "pc"));
        Assert.NotNull(browser.Cookie(OperationsCookies.Device));
    }

    private static async Task SignInWithRecoveryCodeAsync(CookieClient browser, string recoveryCode)
    {
        (HttpResponseMessage _, string page) = await browser.GetAsync("/staff");
        await browser.PostAsync("/staff/sign-in", page, ("email", "grace@deviceco.example"), ("password", Password));
        (HttpResponseMessage _, string recovery) = await browser.GetAsync("/staff/sign-in/recovery");
        await browser.PostAsync("/staff/sign-in/recovery", recovery, ("code", recoveryCode));
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
    }

    // The Owner accepts the invitation and enrols an authenticator app; returns the browser and the app key.
    private async Task<(CookieClient Browser, string Key)> OwnerAsync(Guid tenant, string slug)
    {
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        string key = ManualKey.Match(enrol).Groups["key"].Value;
        (HttpResponseMessage _, string codes) = await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(key, DateTime.UtcNow)));
        _recoveryCodes = RecoveryCode.Matches(codes).Select(match => match.Groups["code"].Value).ToList();
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
        return (browser, key);
    }
}
