using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicSiteHttpTests.cs
//
// PURPOSE:
// Proves the public site on its real pipeline (Stage 2 "Done when").
//
// RESPONSIBILITIES:
// - Languages: the root sends the visitor to their language; pages render in en, fr and es
// - Headers: the CSP allows only the bot check and the card checkout; every cookie is a Secure __Host- cookie
// - Sign-up stays closed until Stage 3 (D-U4), except for a visitor with an offer link
// - The contact form reaches Platform with a signed visitor address, needs its antiforgery token and is limited (a post without the token is refused before the limiter counts it)
// - Pay links, robots and the sitemap
//
// IMPORTANT NOTES:
// - Platform.Api is a stand-in: see PublicSiteTestHost
// =========================================================
public sealed class PublicSiteHttpTests
{
    private const string PlansJson = """
        [{"code":"standard","name":"Standard","description":null,"currencyCode":"USD","monthlyPricePerLicence":25.00,"annualPricePerLicence":250.00,"setupFee":0,"minimumLicences":3,"maxLicensedDevices":25,"maxWarehouses":2,"features":["data_export"],"trialDays":14}]
        """;

    [Theory]
    [InlineData("fr", "/fr/")]
    [InlineData("es-MX,es;q=0.9", "/es/")]
    [InlineData("de", "/en/")]
    public async Task Root_SendsTheVisitorToTheirLanguage(string acceptLanguage, string expected)
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);

        HttpResponseMessage response = await host.Client().SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/en/", "en", "Run shipments, dispatch, warehouses and billing from one system.")]
    [InlineData("/fr/features", "fr", "Fonctionnalités")]
    [InlineData("/es/plans", "es", "Standard")]
    [InlineData("/fr/contact", "fr", "Parlez à notre équipe")]
    public async Task MarketingPage_RendersInItsLanguage_AsPlainHtml(string path, string language, string expected)
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        host.Platform.Answer(HttpMethod.Get, "registration/plans", HttpStatusCode.OK, PlansJson);

        HttpResponseMessage response = await host.Client().GetAsync(path);
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"<html lang=\"{language}\"", page, StringComparison.Ordinal);
        Assert.Contains(expected, WebUtility.HtmlDecode(page), StringComparison.Ordinal);
        Assert.DoesNotContain("blazor.web.js", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryPage_CarriesTheSecurityHeaders_WithACspForTheBotCheckAndCheckoutOnly()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync("/en/features");
        string policy = Header(response, "Content-Security-Policy");

        Assert.Contains("script-src 'self' https://challenges.cloudflare.com;", policy, StringComparison.Ordinal);
        Assert.Contains("form-action 'self' https://checkout.stripe.com;", policy, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-inline", policy, StringComparison.Ordinal);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
    }

    [Fact]
    public async Task EveryCookie_IsAHostCookie_ThatBrowsersKeep()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync("/fr/contact");
        string[] cookies = response.Headers.GetValues("Set-Cookie").ToArray();

        Assert.Contains(cookies, cookie => cookie.StartsWith("__Host-vx4-public-af=", StringComparison.Ordinal));
        Assert.Contains(cookies, cookie => cookie.StartsWith("__Host-vx4-culture=", StringComparison.Ordinal));
        Assert.All(cookies, cookie =>
        {
            string lower = cookie.ToLowerInvariant();
            Assert.StartsWith("__host-", lower, StringComparison.Ordinal);
            Assert.Contains("; secure", lower, StringComparison.Ordinal);
            Assert.Contains("; path=/", lower, StringComparison.Ordinal);
            Assert.Contains("; httponly", lower, StringComparison.Ordinal);
            Assert.DoesNotContain("domain=", lower, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task PlansPage_WhenPlatformIsDown_StillAnswers_WithTheUnavailableMessage()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync("/en/plans");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("We cannot reach our service right now.", page, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/register")]
    [InlineData("/fr/register/plan")]
    [InlineData("/es/register/pay")]
    public async Task SignUp_WhileClosed_ShowsOpensSoon_AndNeverCallsPlatform(string path)
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync(path);
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(path[1..3] == "fr" ? "Les inscriptions ouvrent bientôt" : path[1..3] == "es" ? "El registro abre pronto" : "Sign-up opens soon", WebUtility.HtmlDecode(page), StringComparison.Ordinal);
        Assert.DoesNotContain(host.Platform.Requests, request => request.Path.StartsWith("registration", StringComparison.Ordinal) && request.Path != "registration/plans");
        Assert.DoesNotContain("name=\"Form.LegalName\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignUp_WhileClosed_OpensForAnInvitedVisitor()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        string page = WebUtility.HtmlDecode(await (await host.Client().GetAsync("/en/register?offer=link-from-sales")).Content.ReadAsStringAsync());

        Assert.Contains("Legal name", page, StringComparison.Ordinal);
        Assert.Contains("value=\"link-from-sales\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Sign-up opens soon", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OfferLink_WithoutALanguage_GoesToTheVisitorsLanguage_KeepingTheOffer()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, "/register?offer=abc123");
        request.Headers.TryAddWithoutValidation("Accept-Language", "fr");

        HttpResponseMessage response = await host.Client().SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/fr/register?offer=abc123", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SignUp_WhenOpened_ShowsTheForm()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync(registrationOpen: true);

        string page = await (await host.Client().GetAsync("/en/register")).Content.ReadAsStringAsync();

        Assert.Contains("Legal name", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Contact_Post_SendsTheLeadToPlatform_WithASignedVisitorAddress()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        host.Platform.Answer(HttpMethod.Post, "leads", HttpStatusCode.Accepted, "{}");
        HttpClient client = host.Client();

        (Dictionary<string, string> form, string cookies) = await OpenFormAsync(client, "/fr/contact");
        form["Form.CompanyName"] = "Rivera Freight";
        form["Form.ContactName"] = "Ana Rivera";
        form["Form.Email"] = "ana@rivera.example";
        form["Form.Kind"] = "freight";

        HttpResponseMessage response = await PostAsync(client, "/fr/contact", form, cookies);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(PublicSiteTestHost.BaseAddress + "/fr/contact?sent=1", response.Headers.Location!.OriginalString);

        StubPlatformRequest lead = Assert.Single(host.Platform.Requests, request => request.Path == "leads");
        using JsonDocument body = JsonDocument.Parse(lead.Body);
        Assert.Equal("Rivera Freight", body.RootElement.GetProperty("companyName").GetString());
        Assert.Equal("freight", body.RootElement.GetProperty("kind").GetString());
        Assert.Equal("fr", body.RootElement.GetProperty("language").GetString());

        using JsonDocument caller = VerifiedCaller(lead.Caller!, host.CallerKey);
        Assert.Equal("public", caller.RootElement.GetProperty("iss").GetString());
        Assert.Equal(VisitorAddressStartupFilter.Visitor.ToString(), caller.RootElement.GetProperty("ip").GetString());
    }

    [Fact]
    public async Task Contact_PostWithoutTheAntiforgeryToken_IsRefused_AndNothingReachesPlatform()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        HttpResponseMessage response = await host.Client().PostAsync("/en/contact", ContactForm());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(host.Platform.Requests, request => request.Path == "leads");
    }

    [Fact]
    public async Task Contact_TheSixthPostInAnHour_IsRateLimited_WithAPlainAnswer()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        host.Platform.Answer(HttpMethod.Post, "leads", HttpStatusCode.Accepted, "{}");
        HttpClient client = host.Client();
        List<HttpResponseMessage> responses = new();

        for (int attempt = 0; attempt < 6; attempt++)
        {
            (Dictionary<string, string> form, string cookies) = await OpenFormAsync(client, "/en/contact");
            form["Form.CompanyName"] = "Rivera Freight";
            form["Form.ContactName"] = "Ana Rivera";
            form["Form.Email"] = "ana@rivera.example";
            responses.Add(await PostAsync(client, "/en/contact", form, cookies));
        }

        Assert.DoesNotContain(responses.Take(5), response => response.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[5].StatusCode);
        Assert.Equal("Too many attempts. Please wait a while and try again.", await responses[5].Content.ReadAsStringAsync());
        Assert.Equal(5, host.Platform.Requests.Count(request => request.Path == "leads"));
    }

    [Fact]
    public async Task PayLink_ShowsTheInvoice_AndTheCardOption()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        host.Platform.Answer(HttpMethod.Get, "payment-links/tok-123", HttpStatusCode.OK, """
            {"kind":"pay_invoice","tenantName":"Rivera Freight","invoiceNumber":"INV-2026-0042","balance":125.50,"currencyCode":"USD","dueUtc":"2026-11-01T00:00:00Z","settled":false,"cardPaymentsAvailable":true,"consentTextVersion":"2026-10","zelle":null}
            """);

        HttpResponseMessage response = await host.Client().GetAsync("/pay/tok-123?lang=en");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/pay/tok-123", response.Headers.Location!.OriginalString);

        using HttpRequestMessage again = new(HttpMethod.Get, "/pay/tok-123");
        again.Headers.TryAddWithoutValidation("Cookie", Cookies(response));
        string page = await (await host.Client().SendAsync(again)).Content.ReadAsStringAsync();

        Assert.Contains("INV-2026-0042", page, StringComparison.Ordinal);
        Assert.Contains("Continue to payment", page, StringComparison.Ordinal);
        Assert.Contains("noindex", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PayLink_ForASettledInvoice_SaysItIsPaid()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        host.Platform.Answer(HttpMethod.Get, "payment-links/tok-paid", HttpStatusCode.OK, """
            {"kind":"pay_invoice","tenantName":"Rivera Freight","invoiceNumber":"INV-2026-0041","balance":0,"currencyCode":"USD","dueUtc":null,"settled":true,"cardPaymentsAvailable":true,"consentTextVersion":"2026-10","zelle":null}
            """);

        string page = await (await host.Client().GetAsync("/pay/tok-paid")).Content.ReadAsStringAsync();

        Assert.Contains("This invoice is paid. Thank you.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Continue to payment", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RobotsAndSitemap_ListTheMarketingPages_AndHidePrivateOnes()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        HttpClient client = host.Client();

        string robots = await client.GetStringAsync("/robots.txt");
        string sitemap = await client.GetStringAsync("/sitemap.xml");

        Assert.Contains("Disallow: /pay/", robots, StringComparison.Ordinal);
        Assert.Contains("Sitemap: https://vixly.techcrafters.ltd/sitemap.xml", robots, StringComparison.Ordinal);
        Assert.Contains("<loc>https://vixly.techcrafters.ltd/es/plans</loc>", sitemap, StringComparison.Ordinal);
        Assert.Contains("hreflang=\"fr\" href=\"https://vixly.techcrafters.ltd/fr/contact\"", sitemap, StringComparison.Ordinal);
        Assert.DoesNotContain("register", sitemap, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownPage_AnswersNotFound_WithThePage()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync("/en/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private static FormUrlEncodedContent ContactForm()
    {
        return new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "contact",
            ["Form.CompanyName"] = "Rivera Freight",
            ["Form.ContactName"] = "Ana Rivera",
            ["Form.Email"] = "ana@rivera.example",
            ["Form.Kind"] = "freight"
        });
    }

    private static async Task<(Dictionary<string, string> Form, string Cookies)> OpenFormAsync(HttpClient client, string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        string page = await response.Content.ReadAsStringAsync();
        Dictionary<string, string> form = new(StringComparer.Ordinal);

        foreach (Match hidden in Regex.Matches(page, "<input[^>]*type=\"hidden\"[^>]*>"))
        {
            string name = Regex.Match(hidden.Value, "name=\"([^\"]+)\"").Groups[1].Value;
            string value = Regex.Match(hidden.Value, "value=\"([^\"]*)\"").Groups[1].Value;
            form[name] = WebUtility.HtmlDecode(value);
        }

        Assert.True(form.ContainsKey("__RequestVerificationToken"), "The form carries no antiforgery token.");

        return (form, Cookies(response));
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Dictionary<string, string> form, string cookies)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(form) };
        request.Headers.TryAddWithoutValidation("Cookie", cookies);

        return await client.SendAsync(request);
    }

    private static string Cookies(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
            ? string.Join("; ", values.Select(value => value.Split(';')[0]))
            : string.Empty;
    }

    private static JsonDocument VerifiedCaller(string token, ECDsa key)
    {
        string[] parts = token.Split('.');

        Assert.Equal(3, parts.Length);
        Assert.True(
            key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Base64Url.DecodeFromChars(parts[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation),
            "The caller token signature does not verify.");

        return JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : string.Empty;
    }
}
