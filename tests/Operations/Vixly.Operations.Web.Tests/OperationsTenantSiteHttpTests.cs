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
// FILE: OperationsTenantSiteHttpTests.cs
//
// PURPOSE:
// Proves the tenant public site and its editor end to end (Slice 1B).
//
// RESPONSIBILITIES:
// - Two tenants show two sites: name, texts, SEO, JSON-LD, robots and sitemap; never "Vixly"
// - The draft is invisible until published; preview is for the signed-in Owner only
// - Locations appear under the country of their own address; every value is encoded
// - Images: SVG refused, PNG accepted, served only once published, with nosniff
// - Colours below AA are refused; tracking answers not found; the editor needs a sign-in
// - The Development demo seed invites the owner of each active tenant once
//
// IMPORTANT NOTES:
// - Real pipeline, real database (VIXLY_TEST_SQL_SERVER); Platform and mail are stood in
// =========================================================
public sealed class OperationsTenantSiteHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private static readonly Regex ManualKey = new("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant);

    private static readonly Regex ImageId = new("/site/image/(?<id>[0-9a-f]{32})", RegexOptions.CultureInvariant);

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsTenantSiteHttpTests(OperationsWebDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync() => _host = await OperationsWebTestHost.StartAsync(_database.ConnectionString);

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [SqlServerFact]
    public async Task TwoTenants_ShowTwoSites_AndNeverVixly()
    {
        using CookieClient alpha = await OwnerAsync("alphacargo", "Alpha Cargo", "en");
        using CookieClient beta = await OwnerAsync("betaenvios", "Beta Envios", "es");

        await SaveSiteAsync(alpha, "en", ("text.headline", "Alpha ships to Haiti"), ("text.meta_description", "Alpha Cargo: Miami to Port-au-Prince in 5 days."));
        await PublishAsync(alpha);

        HttpClient visitor = _host.Client("alphacargo.techcrafters.ltd");
        HttpResponseMessage root = await visitor.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, root.StatusCode);
        Assert.Equal("/en", root.Headers.Location?.OriginalString);
        Assert.Equal("/es", (await _host.Client("betaenvios.techcrafters.ltd").GetAsync("/")).Headers.Location?.OriginalString);

        HttpResponseMessage home = await visitor.GetAsync("/en");
        string html = await home.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("<title>Alpha Cargo | Alpha ships to Haiti</title>", html, StringComparison.Ordinal);
        Assert.Contains("content=\"Alpha Cargo: Miami to Port-au-Prince in 5 days.\"", html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"canonical\" href=\"https://alphacargo.techcrafters.ltd/en\"", html, StringComparison.Ordinal);
        Assert.Contains("hreflang=\"fr\" href=\"https://alphacargo.techcrafters.ltd/fr\"", html, StringComparison.Ordinal);
        Assert.Contains("\"@type\":\"Organization\"", html, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"Alpha Cargo\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/customer/sign-up\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/customer/sign-in\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/staff\"", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "href=\"/customer/sign-in\""));
        Assert.DoesNotContain("Beta", html, StringComparison.Ordinal);
        NoVixly(home, html);

        string spanish = await (await _host.Client("betaenvios.techcrafters.ltd").GetAsync("/es")).Content.ReadAsStringAsync();
        Assert.Contains("lang=\"es\"", spanish, StringComparison.Ordinal);
        Assert.Contains("Envíe con Beta Envios", System.Net.WebUtility.HtmlDecode(spanish), StringComparison.Ordinal);
        Assert.DoesNotContain("Alpha", spanish, StringComparison.Ordinal);

        HttpResponseMessage robots = await visitor.GetAsync("/robots.txt");
        string robotsText = await robots.Content.ReadAsStringAsync();
        Assert.Contains("Disallow: /staff", robotsText, StringComparison.Ordinal);
        Assert.Contains("Sitemap: https://alphacargo.techcrafters.ltd/sitemap.xml", robotsText, StringComparison.Ordinal);
        NoVixly(robots, robotsText);

        HttpResponseMessage sitemap = await visitor.GetAsync("/sitemap.xml");
        string sitemapXml = await sitemap.Content.ReadAsStringAsync();
        Assert.Contains("<loc>https://alphacargo.techcrafters.ltd/es/locations</loc>", sitemapXml, StringComparison.Ordinal);
        Assert.DoesNotContain("betaenvios", sitemapXml, StringComparison.Ordinal);
        NoVixly(sitemap, sitemapXml);
    }

    [SqlServerFact]
    public async Task Draft_IsInvisible_UntilPublished_AndPreviewIsForTheOwner()
    {
        using CookieClient owner = await OwnerAsync("draftco", "Draft Co", "en");
        await SaveSiteAsync(owner, "en", ("text.headline", "Brand new headline"));

        HttpClient visitor = _host.Client("draftco.techcrafters.ltd");
        string before = await (await visitor.GetAsync("/en?preview=true")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Brand new headline", before, StringComparison.Ordinal);
        Assert.DoesNotContain("noindex", before, StringComparison.Ordinal);

        (HttpResponseMessage previewResponse, string preview) = await owner.GetAsync("/en?preview=true");
        Assert.Contains("Brand new headline", preview, StringComparison.Ordinal);
        Assert.Contains("noindex", preview, StringComparison.Ordinal);
        Assert.Contains("no-store", previewResponse.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.Ordinal);

        await PublishAsync(owner);

        Assert.Contains("Brand new headline", await (await visitor.GetAsync("/en")).Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // Prices on the public website (owner, 2026-10-21): only while the company publishes prices, only the regular lists marked to publish, never a customer's.
    [SqlServerFact]
    public async Task Pricing_OnThePublicWebsite_OnlyWhatTheCompanyPublishes()
    {
        using CookieClient owner = await OwnerAsync("priceco", "Price Co", "en");
        (HttpResponseMessage _, string services) = await owner.GetAsync("/staff/services");
        Assert.Equal("/staff/services?done=1", (await owner.PostAsync("/staff/services", services, ("offered", "air_standard"), ("offered", "air_express"), ("label_air_standard", "Air Miami"),
            ("min_air_standard", "3"), ("max_air_standard", "5"), ("divisor_air_standard", "5000"), ("min_air_express", "2"), ("max_air_express", "3"), ("divisor_air_express", "6000"),
            ("min_ocean_consolidated", "14"), ("max_ocean_consolidated", "28"), ("divisor_ocean_consolidated", "1000"), ("min_ocean_full_container", "14"),
            ("max_ocean_full_container", "35"), ("min_ground", "1"), ("max_ground", "2"), ("divisor_ground", "5000"))).Response.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string charges) = await owner.GetAsync("/staff/charges");
        // Never on the website: a regular list the company does not publish, and a customer's own list even when posted as published.
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/staff/charges/cards", charges, ("mode", "Air"), ("active", "true"),
            ("service", "air_express"), ("band_from_0", "0"), ("band_kind_0", "PerUnit"), ("band_amount_0", "7.77"))).Response.StatusCode);
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            Guid tenant = _host.Platform.Portals["priceco"].TenantId!.Value;
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsCustomer customer = OperationsCustomer.SignUp(tenant, "Ana", "Pierre", null, EmailAddress.Create("ana@priceco-buyer.example"), "+50937123456", LanguageCode.En, "$2a$12$hash", DateTimeOffset.UtcNow);
            customer.ConfirmEmail("C00001", DateTimeOffset.UtcNow);
            scope.ServiceProvider.GetRequiredService<IOperationsCustomerRepository>().Add(customer);
            await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/staff/charges/cards", charges, ("mode", "Air"), ("active", "true"), ("published", "true"), ("customer", "C00001"),
            ("service", "air_standard"), ("band_from_0", "0"), ("band_kind_0", "PerUnit"), ("band_amount_0", "9.99"))).Response.StatusCode);

        (HttpResponseMessage _, string settings) = await owner.GetAsync("/staff/settings");
        string form = settings[settings.IndexOf("action=\"/staff/settings\"", StringComparison.Ordinal)..];
        form = form[..form.IndexOf("</form>", StringComparison.Ordinal)];
        List<(string, string)> fields = Regex.Matches(form, "<input(?=[^>]*name=\"(?<n>[^\"]+)\")(?=[^>]*value=\"(?<v>[^\"]*)\")(?<rest>[^>]*)>")
            .Where(match => !match.Value.Contains("type=\"checkbox\"", StringComparison.Ordinal) || match.Value.Contains("checked", StringComparison.Ordinal))
            .Select(match => (match.Groups["n"].Value, WebUtility.HtmlDecode(match.Groups["v"].Value))).Where(field => field.Item1 != "__RequestVerificationToken").ToList();
        fields.AddRange(Regex.Matches(form, "<select name=\"(?<n>[^\"]+)\"[\\s\\S]*?<option value=\"(?<v>[^\"]*)\" selected").Select(match => (match.Groups["n"].Value, match.Groups["v"].Value)));
        fields.Add(("publicPricing", "true"));
        (HttpResponseMessage settingsSaved, string _) = await owner.PostAsync("/staff/settings", settings, fields.ToArray());
        Assert.Equal(HttpStatusCode.Redirect, settingsSaved.StatusCode);

        // Prices public, but only an unpublished list and a customer's own: no page, no menu link, not in the sitemap.
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetRawAsync("/en/pricing")).Response.StatusCode);
        Assert.DoesNotContain("/en/pricing", (await owner.GetRawAsync("/en")).Raw, StringComparison.Ordinal);
        Assert.DoesNotContain("/en/pricing", (await owner.GetRawAsync("/sitemap.xml")).Raw, StringComparison.Ordinal);

        (HttpResponseMessage saved, string _) = await owner.PostAsync("/staff/charges/cards", charges, ("mode", "Air"), ("active", "true"), ("published", "true"),
            ("service", "air_standard"), ("band_from_0", "0"), ("band_kind_0", "PerUnit"), ("band_amount_0", "3.25"));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        (HttpResponseMessage page, string raw) = await owner.GetRawAsync("/en/pricing");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("3.25", raw, StringComparison.Ordinal);
        Assert.Contains("Air Miami", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("7.77", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("9.99", raw, StringComparison.Ordinal);
        Assert.Contains("/en/pricing", (await owner.GetRawAsync("/en")).Raw, StringComparison.Ordinal);
        Assert.Contains("/en/pricing", (await owner.GetRawAsync("/sitemap.xml")).Raw, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Locations_AppearUnderTheirOwnCountry_AndEveryValueIsEncoded()
    {
        using CookieClient owner = await OwnerAsync("haitiexpress", "Haiti Express", "en");

        (HttpResponseMessage _, string branchForm) = await owner.GetAsync("/staff/locations/branch");
        (HttpResponseMessage branchSaved, string _) = await owner.PostAsync("/staff/locations/branch", branchForm,
            ("code", "PAP"), ("name", "Port-au-Prince office"), ("line1", "12 Rue Capois"), ("city", "Port-au-Prince"), ("countryCode", "HT"),
            ("phone", "+50922223333"), ("side", "Destination"), ("status", "Active"), ("showOnSite", "true"));
        Assert.Equal(HttpStatusCode.Redirect, branchSaved.StatusCode);

        (HttpResponseMessage _, string warehouseForm) = await owner.GetAsync("/staff/locations/warehouse");
        string branchId = Regex.Match(warehouseForm, "<option value=\"(?<id>[0-9a-f-]{36})\"").Groups["id"].Value;
        (HttpResponseMessage warehouseSaved, string _) = await owner.PostAsync("/staff/locations/warehouse", warehouseForm,
            ("branchId", branchId), ("code", "FLL"), ("name", "<script>alert(1)</script> Fort Lauderdale"), ("line1", "1 Harbor Road"), ("city", "Fort Lauderdale"),
            ("region", "FL"), ("countryCode", "US"), ("roles", "Receiving"), ("acceptingDeliveries", "true"), ("status", "Active"), ("showOnSite", "true"));
        Assert.Equal(HttpStatusCode.Redirect, warehouseSaved.StatusCode);

        (HttpResponseMessage _, string contactForm) = await owner.GetAsync("/staff/locations/contact");
        (HttpResponseMessage contactSaved, string _) = await owner.PostAsync("/staff/locations/contact", contactForm, ("countryCode", "HT"), ("phones", "+509 4860-5107\n+50939946752\n+509 3214 9800"), ("whatsApp", "+50933334444"), ("sortOrder", "1"));
        Assert.Equal(HttpStatusCode.Redirect, contactSaved.StatusCode);

        (HttpResponseMessage _, string refusedPage) = await owner.PostAsync("/staff/locations/contact", contactForm, ("countryCode", "HT"), ("phones", "+50948605107\ncall us"));
        Assert.Contains("call us is not a phone number in international form", refusedPage, StringComparison.Ordinal);

        (HttpResponseMessage _, string raw) = await owner.GetRawAsync("/en/locations");
        string haiti = WebUtility.HtmlDecode(Section(raw, "ht"));
        string unitedStates = WebUtility.HtmlDecode(Section(raw, "us"));

        Assert.Contains("Port-au-Prince office", haiti, StringComparison.Ordinal);
        Assert.Contains("href=\"tel:+50948605107\">+509 4860-5107</a>", haiti, StringComparison.Ordinal);
        Assert.Contains("tel:+50939946752", haiti, StringComparison.Ordinal);
        Assert.Contains("tel:+50932149800", haiti, StringComparison.Ordinal);
        Assert.DoesNotContain("50922223333", raw, StringComparison.Ordinal);
        Assert.Contains("https://wa.me/50933334444", haiti, StringComparison.Ordinal);
        Assert.DoesNotContain("Fort Lauderdale", haiti, StringComparison.Ordinal);
        Assert.Contains("Fort Lauderdale", unitedStates, StringComparison.Ordinal);
        Assert.Contains("Accepting deliveries", unitedStates, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>alert(1)", raw, StringComparison.Ordinal);
        Assert.Contains("\"addressCountry\":\"US\"", raw, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Images_AreCheckedByContent_AndServedOnlyOncePublished()
    {
        using CookieClient owner = await OwnerAsync("pictureco", "Picture Co", "en");
        (HttpResponseMessage _, string editor) = await owner.GetAsync("/staff/site");

        (HttpResponseMessage svgResponse, string svg) = await owner.PostFileAsync("/staff/site/image/logo", editor, "logo.svg", "image/png",
            System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"/>"));
        Assert.Equal(HttpStatusCode.OK, svgResponse.StatusCode);
        Assert.Contains("SVG is not accepted", svg, StringComparison.Ordinal);

        (HttpResponseMessage uploaded, string _) = await owner.PostFileAsync("/staff/site/image/logo", editor, "logo.png", "image/png", Png(240, 80));
        Assert.Equal(HttpStatusCode.Redirect, uploaded.StatusCode);

        (HttpResponseMessage _, string withImage) = await owner.GetAsync("/staff/site");
        string path = "/site/image/" + ImageId.Match(withImage).Groups["id"].Value;
        HttpClient visitor = _host.Client("pictureco.techcrafters.ltd");
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path)).Response.StatusCode);

        await PublishAsync(owner);

        HttpResponseMessage image = await visitor.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("immutable", image.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(path, await (await visitor.GetAsync("/en")).Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task LowContrast_AndMarkup_AreRefused_WithTheReason()
    {
        using CookieClient owner = await OwnerAsync("colourco", "Colour Co", "en");

        string light = await SaveSiteAsync(owner, "en", expectSaved: false, primary: "#FFE066");
        Assert.Contains("too light", light, StringComparison.Ordinal);

        string markup = await SaveSiteAsync(owner, "en", expectSaved: false, fields: ("text.headline", "<b>Big</b>"));
        Assert.Contains("plain text", markup, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Tracking_AnswersNotFound_ForAnyNumber()
    {
        await OwnerAsync("trackco", "Track Co", "en");

        HttpResponseMessage response = await _host.Client("trackco.techcrafters.ltd").GetAsync("/en/track?number=VX-0001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No shipment matches this number", System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()), StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Editor_NeedsASignIn_AndATenantBeingSetUpHasNoPublicSite()
    {
        NewTenant("lockedco", "Locked Co", "active", "en");
        HttpResponseMessage editor = await _host.Client("lockedco.techcrafters.ltd").GetAsync("/staff/site");
        Assert.Equal(HttpStatusCode.Redirect, editor.StatusCode);
        Assert.Equal("/staff", editor.Headers.Location?.OriginalString);

        NewTenant("setupco", "Setup Co", "draft", "en");
        HttpClient setup = _host.Client("setupco.techcrafters.ltd");
        Assert.Equal("/setup", (await setup.GetAsync("/en")).Headers.Location?.OriginalString);
        Assert.Equal("User-agent: *\nDisallow: /\n", await (await setup.GetAsync("/robots.txt")).Content.ReadAsStringAsync());
    }

    [SqlServerFact]
    public async Task DemoSeed_InvitesTheOwnerOfEachActiveTenant_Once()
    {
        NewTenant("demo-live", "Demo Live", "active", "fr");
        NewTenant("demo-draft", "Demo Draft", "draft", "en");

        await _host.RunDemoSeedAsync("demo-live", "demo-draft", "demo-unknown");
        await _host.RunDemoSeedAsync("demo-live");

        Assert.Single(_host.Mailer.Invitations, invitation => invitation.Link.Contains("demo-live", StringComparison.Ordinal));
        Assert.DoesNotContain(_host.Mailer.Invitations, invitation => invitation.Link.Contains("demo-draft", StringComparison.Ordinal));
        Assert.Contains("Demo Live", System.Net.WebUtility.HtmlDecode(await (await _host.Client("demo-live.techcrafters.ltd").GetAsync("/fr")).Content.ReadAsStringAsync()), StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task DemoSeed_GivesStaffWhoCanSignIn_WithTheDemoPasswordAndKey()
    {
        NewTenant("demo-staff", "Demo Staff", "active", "en");
        OperationsDemoDataOptions demo = new();

        await _host.RunDemoSeedAsync("demo-staff");
        await _host.RunDemoSeedAsync("demo-staff");

        // The Owner and the Administrator need the code from the demo key; the front desk only the password.
        foreach (string email in new[] { "grace@demo-staff.example", "admin@demo-staff.example" })
        {
            using CookieClient browser = _host.Browser("demo-staff.techcrafters.ltd");
            (HttpResponseMessage _, string landing) = await browser.GetAsync("/staff");
            (HttpResponseMessage password, string _) = await browser.PostAsync("/staff/sign-in", landing, ("email", email), ("password", demo.Password));
            Assert.StartsWith("/staff/sign-in/verify", password.Headers.Location?.OriginalString, StringComparison.Ordinal);
            (HttpResponseMessage _, string verify) = await browser.GetAsync(password.Headers.Location!.OriginalString);
            await browser.PostAsync("/staff/sign-in/verify", verify, ("code", new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(demo.AuthenticatorKey)).ComputeTotp(DateTime.UtcNow)));
            Assert.NotNull(browser.Cookie(OperationsCookies.Session));
        }

        using CookieClient desk = _host.Browser("demo-staff.techcrafters.ltd");
        (HttpResponseMessage _, string page) = await desk.GetAsync("/staff");
        await desk.PostAsync("/staff/sign-in", page, ("email", "staff@demo-staff.example"), ("password", demo.Password));
        Assert.NotNull(desk.Cookie(OperationsCookies.Session));

        // The demo customer is active with a code and the demo password, once only.
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(_host.Platform.Portals["demo-staff"].TenantId!.Value);
        OperationsCustomer customer = (await scope.ServiceProvider.GetRequiredService<IOperationsCustomerRepository>()
            .FindByEmailAsync(EmailAddress.Create("customer@demo-staff.example"), CancellationToken.None))!;
        Assert.Equal("C00001", customer.Code);
        Assert.True(customer.CanSignIn(DateTimeOffset.UtcNow));
        Assert.Equal(OperationsPasswordCheck.Succeeded,
            scope.ServiceProvider.GetRequiredService<IOperationsPasswordHasher>().Verify(demo.Password, customer.PasswordHash!));
    }

    private static void NoVixly(HttpResponseMessage response, string body)
    {
        // The shared UI library's asset addresses (folder and file names) are plumbing, not text a visitor sees (owner, 2026-10-18).
        string visible = System.Text.RegularExpressions.Regex.Replace(body, @"/?_content/Vixly\.Shared\.UI/[A-Za-z0-9._-]+", string.Empty);
        Assert.DoesNotContain("vixly", visible, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(response.Headers.Concat(response.Content.Headers), header => header.Value.Any(value => value.Contains("vixly", StringComparison.OrdinalIgnoreCase)));
    }

    private static string Section(string html, string country)
    {
        int start = html.IndexOf("id=\"country-" + country + "\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "No section for " + country);
        int next = html.IndexOf("id=\"country-", start + 10, StringComparison.Ordinal);
        return next < 0 ? html[start..] : html[start..next];
    }

    private async Task<string> SaveSiteAsync(CookieClient owner, string language, params (string Name, string Value)[] fields) =>
        await SaveSiteAsync(owner, language, true, "#1D4ED8", fields);

    private async Task<string> SaveSiteAsync(CookieClient owner, string language, bool expectSaved, string primary = "#1D4ED8", params (string Name, string Value)[] fields)
    {
        (HttpResponseMessage _, string editor) = await owner.GetAsync("/staff/site?language=" + language);
        string company = Regex.Match(editor, "name=\"displayName\" value=\"(?<name>[^\"]+)\"").Groups["name"].Value;
        (string, string)[] form = new[] { ("displayName", company), ("preset", "Ocean"), ("primaryColour", primary), ("accentColour", "#0E7490"), ("services", "air"), ("showSteps", "true") }
            .Concat(fields).ToArray();

        (HttpResponseMessage response, string html) = await owner.PostAsync("/staff/site?language=" + language, editor, form);
        Assert.Equal(expectSaved ? HttpStatusCode.Redirect : HttpStatusCode.OK, response.StatusCode);
        return html;
    }

    private static async Task PublishAsync(CookieClient owner)
    {
        (HttpResponseMessage _, string editor) = await owner.GetAsync("/staff/site");
        (HttpResponseMessage published, string _) = await owner.PostAsync("/staff/site/publish", editor);
        Assert.Equal("/staff/site?saved=published", published.Headers.Location?.OriginalString);
    }

    private Guid NewTenant(string slug, string company, string status, string language)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, status, language: language);
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, language, "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    // A live tenant whose Owner has accepted the invitation and enrolled an authenticator app.
    private async Task<CookieClient> OwnerAsync(string slug, string company, string language)
    {
        Guid tenant = NewTenant(slug, company, "active", language);
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        string code = new Totp(Base32Encoding.ToBytes(ManualKey.Match(enrol).Groups["key"].Value)).ComputeTotp(DateTime.UtcNow);
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", code));
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));

        // A live tenant's staff work only from a licensed device (Slice 1C).
        (HttpResponseMessage _, string devicePage) = await browser.GetAsync("/devices/enrol");
        await browser.PostAsync("/devices/enrol", devicePage, ("name", "Owner laptop"), ("kind", "pc"));
        Assert.NotNull(browser.Cookie(OperationsCookies.Device));
        return browser;
    }

    // A PNG header: signature, then the IHDR chunk with the size.
    private static byte[] Png(int width, int height)
    {
        byte[] data = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(data, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(20), height);
        return data;
    }
}
