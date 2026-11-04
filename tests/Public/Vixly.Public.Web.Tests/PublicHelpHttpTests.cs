using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Public.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicHelpHttpTests.cs
//
// PURPOSE:
// Verifies Public help on the real anonymous HTTP pipeline.
//
// RESPONSIBILITIES:
// - Check all languages, endpoint metadata, navigation and sitemap.
// - Keep search uncached and unknown article addresses at 404.
//
// IMPORTANT NOTES:
// - Platform is a test stand-in; help must work without calling it.
// =========================================================
public sealed class PublicHelpHttpTests
{
    [Theory]
    [InlineData("en", "Vixly help")]
    [InlineData("fr", "Aide Vixly")]
    [InlineData("es", "Ayuda de Vixly")]
    public async Task AllArticlesAndHomeOpenSignedOutWhileRegistrationIsClosed(string language, string homeTitle)
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpClient client = host.Client();
        PublicHelpCatalogue help = new();
        Assert.Equal(16, help.Catalogue.Visible(language, PublicHelpCatalogue.Reader).Count);
        Assert.Empty(HelpCheck.Run(help.Documents, PublicHelpCatalogue.Options(host.Routes)));
        HttpResponseMessage home = await client.GetAsync($"/{language}/help");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        string html = WebUtility.HtmlDecode(await home.Content.ReadAsStringAsync());
        Assert.Contains(homeTitle, html);
        Assert.Contains($"<html lang=\"{language}\"", html);
        foreach (HelpArticle article in help.Catalogue.Visible(language, PublicHelpCatalogue.Reader))
        {
            string address = PublicHelpCatalogue.Address(language, article.Metadata.Slug);
            Assert.Contains(address, html);
            HttpResponseMessage response = await client.GetAsync(address);
            string body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(article.Metadata.Title, body);
            Assert.Contains("<article class=\"vx-help-article\"", body);
            Assert.DoesNotContain("blazor.web.js", body);
            Assert.DoesNotContain("sources:", body);
            foreach (string alternate in new[] { "en", "fr", "es" })
                Assert.Contains(PublicHelpCatalogue.Address(alternate, article.Metadata.Slug), body);
        }
        // The maintenance check polls in the background on every site; help pages call nothing else.
        Assert.DoesNotContain(host.Platform.Requests, request => !request.Path.StartsWith("maintenance", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task UnknownArticleUsesNormalNotFoundPageAndStatus(string language)
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        HttpResponseMessage response = await host.Client().GetAsync($"/{language}/help/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("noindex", html);
        Assert.DoesNotContain("vx-help-article\"", html);
    }

    [Theory]
    [InlineData("/en/register", "en", "sign-up-overview")]
    [InlineData("/fr/register/plan", "fr", "choose-your-plan-at-sign-up")]
    [InlineData("/es/register/email", "es", "confirm-your-email")]
    [InlineData("/register/verify?token=private-verification&lang=fr", "fr", "confirm-your-email")]
    [InlineData("/pay/private-payment", "en", "pay-an-invoice-link")]
    [InlineData("/cards/private-card", "en", "save-a-card")]
    [InlineData("/en/help", "en", null)]
    public async Task HeaderOpensContextArticleWithoutCopyingVisitorTokens(string path, string language, string? slug)
    {
        // Direct catalogue matching also covers email pages without depending on their upstream state.
        PublicHelpCatalogue help = new();
        string requestPath = path.Split('?')[0];
        string expected = slug is null ? PublicHelpCatalogue.Home(language) : PublicHelpCatalogue.Address(language, slug);
        Assert.Equal(expected, help.ContextAddress(language, requestPath));
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpClient client = host.Client();
        using HttpRequestMessage request = new(HttpMethod.Get, path.Replace("&lang=fr", ""));
        request.Headers.AcceptLanguage.ParseAdd(language);
        HttpResponseMessage response = await client.SendAsync(request);
        string html = await response.Content.ReadAsStringAsync();
        Match link = Regex.Match(html, "<a[^>]*class=\"vx-help-link\"[^>]*href=\"([^\"]+)\"");
        Assert.True(link.Success, html);
        Assert.Equal(expected, link.Groups[1].Value);
    }

    [Fact]
    public async Task SitemapIncludesEveryArticleWithAllLanguageAlternates()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        string xml = await host.Client().GetStringAsync("/sitemap.xml");
        XDocument sitemap = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";
        PublicHelpCatalogue help = new();
        foreach (HelpArticle article in help.Catalogue.Visible("en", PublicHelpCatalogue.Reader))
            foreach (string language in new[] { "en", "fr", "es" })
            {
                string address = PublicSiteTestHost.BaseAddress + PublicHelpCatalogue.Address(language, article.Metadata.Slug);
                XElement entry = Assert.Single(sitemap.Descendants(ns + "url"), element => element.Element(ns + "loc")?.Value == address);
                Assert.Equal(3, entry.Elements(xhtml + "link").Count());
                foreach (string alternate in new[] { "en", "fr", "es" })
                    Assert.Contains(entry.Elements(xhtml + "link"), link => (string?)link.Attribute("hreflang") == alternate
                        && (string?)link.Attribute("href") == PublicSiteTestHost.BaseAddress + PublicHelpCatalogue.Address(alternate, article.Metadata.Slug));
            }
    }

    [Fact]
    public async Task SearchIsUncachedEscapedAndDoesNotPolluteHomeCache()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpClient client = host.Client();
        HttpResponseMessage initial = await client.GetAsync("/en/help");
        string cookie = initial.Headers.GetValues("Set-Cookie").First(value => value.StartsWith("__Host-vx4-culture=", StringComparison.Ordinal)).Split(';')[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        _ = await client.GetAsync("/en/help");
        HttpResponseMessage cached = await client.GetAsync("/en/help");
        Assert.NotNull(cached.Headers.Age);
        HttpResponseMessage search = await client.GetAsync("/en/help?q=ZELLE");
        string html = WebUtility.HtmlDecode(await search.Content.ReadAsStringAsync());
        Assert.Null(search.Headers.Age);
        Assert.Contains("Pay by Zelle", html);
        Assert.Contains("noindex", html);
        HttpResponseMessage empty = await client.GetAsync("/en/help?q=nomatchingwordxyz");
        Assert.Contains("No matching articles", await empty.Content.ReadAsStringAsync());
        string escaped = await client.GetStringAsync("/en/help?q=%3Cscript%3Ealert%281%29%3C%2Fscript%3E");
        Assert.DoesNotContain("<script>alert(1)</script>", escaped);
        Assert.Contains("&lt;script&gt;", escaped);
        string home = await client.GetStringAsync("/en/help");
        Assert.Contains("What Vixly does for your company", WebUtility.HtmlDecode(home));
        Assert.DoesNotContain("nomatchingwordxyz", home);
    }

    [Fact]
    public async Task HelpReadsHaveAnEnforcedRateLimit()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpClient client = host.Client();
        HttpResponseMessage? last = null;
        for (int index = 0; index < 121; index++)
        {
            last?.Dispose();
            last = await client.GetAsync("/en/help?q=example");
        }
        using (last) Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }
}
