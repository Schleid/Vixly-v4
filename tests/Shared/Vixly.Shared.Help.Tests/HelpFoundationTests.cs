using System.Diagnostics;
using Vixly.Shared.Help;

namespace Vixly.Shared.Help.Tests;

// =========================================================
// FILE: HelpFoundationTests.cs
//
// PURPOSE:
// Defines or verifies the shared help foundation.
//
// RESPONSIBILITIES:
// - Support the help contract of ADR-0023.
//
// IMPORTANT NOTES:
// - No plane implementation dependencies.
// =========================================================
public sealed class HelpFoundationTests
{
    [Fact]
    public void ParsesAllFieldsAndBlockSources()
    {
        var (metadata, body) = HelpFrontMatter.Parse(HelpFixtures.Markdown(), "en", HelpFixtures.Options);
        Assert.Equal("welcome", metadata.Slug);
        Assert.Equal(new[] { "src/example.cs" }, metadata.Sources);
        Assert.Contains("## Steps", body);
    }

    [Theory]
    [InlineData("slug: welcome", "slug: Bad_slug")]
    [InlineData("title: Café welcome", "title: ")]
    [InlineData("summary: Learn to start.", "summary: ")]
    [InlineData("section: start", "section: missing")]
    [InlineData("order: 1", "order: first")]
    [InlineData("permissions: []", "permissions: read")]
    [InlineData("conditions: []", "conditions: cards")]
    [InlineData("routes: []", "routes: /home")]
    [InlineData("related: []", "related: [a,b,c,d,e,f]")]
    [InlineData("updated: 2026-10-22", "updated: yesterday")]
    [InlineData("review: ok", "review: needs-review")]
    [InlineData("sources:\n  - src/example.cs", "sources: []")]
    public void RejectsInvalidFields(string before, string after) =>
        Assert.Throws<FormatException>(() => HelpFrontMatter.Parse(HelpFixtures.Markdown().Replace(before, after), "en", HelpFixtures.Options));

    [Theory]
    [InlineData("slug")][InlineData("title")][InlineData("summary")][InlineData("section")]
    [InlineData("order")][InlineData("permissions")][InlineData("conditions")][InlineData("routes")]
    [InlineData("related")][InlineData("sources")][InlineData("updated")][InlineData("review")]
    public void EveryRequiredFieldIsChecked(string key)
    {
        string markdown = string.Join('\n', HelpFixtures.Markdown().Split('\n').Where(line => !line.StartsWith(key + ":")));
        Assert.Throws<FormatException>(() => HelpFrontMatter.Parse(markdown, "en", HelpFixtures.Options));
    }

    [Theory]
    [InlineData("slug", 61)][InlineData("title", 71)][InlineData("summary", 161)]
    public void RejectsLongFields(string key, int length)
    {
        string markdown = System.Text.RegularExpressions.Regex.Replace(HelpFixtures.Markdown(), $"(?m)^{key}:.*$", $"{key}: {new string('a', length)}");
        Assert.Throws<FormatException>(() => HelpFrontMatter.Parse(markdown, "en", HelpFixtures.Options));
    }

    [Fact]
    public void RendersTablesAnchorsHelpLinksAndEncodedTokens()
    {
        string html = new HelpRenderer().Render("## Start\n[Read](help:welcome)\n\n{brandName}\n\n|A|B|\n|-|-|\n|1|2|", "fr", HelpFixtures.Options);
        Assert.Contains("/fr/help/welcome", html);
        Assert.Contains("id=\"start\"", html);
        Assert.Contains("<table>", html);
        html = HelpRenderer.ReplaceTokens(html, new Dictionary<string, string> { ["brandName"] = "<b>" });
        Assert.Contains("&lt;b&gt;", html);
        Assert.DoesNotContain("<b>", html);
    }

    [Fact]
    public void RawHtmlIsEscapedAndFailsPublication()
    {
        const string body = "<script>alert(1)</script>";
        Assert.DoesNotContain("<script>", new HelpRenderer().Render(body, "en", HelpFixtures.Options));
        Assert.NotEmpty(HelpCheck.Run(HelpFixtures.Documents(HelpFixtures.Markdown(body: body)), HelpFixtures.Options));
    }

    [Theory]
    [InlineData("[x](javascript:alert)")][InlineData("[x](http://example.com)")]
    [InlineData("[x](//example.com)")][InlineData("[x](data:text/plain,test)")]
    [InlineData("{unknown}")][InlineData("![x](https://example.com/x.png)")]
    [InlineData("# Too high")][InlineData("[x](%2f%2fevil.test)")]
    public void RejectsUnsafeMarkdown(string body) =>
        Assert.Throws<FormatException>(() => new HelpRenderer().Render(body, "en", HelpFixtures.Options));

    [Fact]
    public void ChecksTranslationCompletenessDuplicatesAndRoutes()
    {
        var documents = HelpFixtures.Documents(HelpFixtures.Markdown("welcome").Replace("routes: []", "routes: [/home]"));
        Assert.Empty(HelpCheck.Run(documents, HelpFixtures.Options));
        Assert.NotEmpty(HelpCheck.Run(documents.Take(2), HelpFixtures.Options));
        Assert.NotEmpty(HelpCheck.Run(documents.Concat(documents), HelpFixtures.Options));
        Assert.NotEmpty(HelpCheck.Run(documents.Concat(HelpFixtures.Documents(HelpFixtures.Markdown("other").Replace("routes: []", "routes: [/home]"))), HelpFixtures.Options));
    }

    [Theory]
    [InlineData("permissions: []", "permissions: [unknown]")]
    [InlineData("conditions: []", "conditions: [unknown]")]
    [InlineData("routes: []", "routes: [/unknown]")]
    [InlineData("related: []", "related: [unknown]")]
    [InlineData("## Steps", "## Steps\n[Unknown](help:missing)")]
    public void ChecksReferences(string before, string after) =>
        Assert.NotEmpty(HelpCheck.Run(HelpFixtures.Documents(HelpFixtures.Markdown().Replace(before, after)), HelpFixtures.Options));

    [Fact]
    public void TranslationMustKeepStructureAndReaderMetadata()
    {
        HelpDocument[] documents = HelpFixtures.Documents(HelpFixtures.Markdown());
        documents[1] = documents[1] with { Markdown = documents[1].Markdown.Replace("conditions: []", "conditions: [cards]").Replace("## Steps", "## Steps\n## Extra") };
        Assert.Contains(HelpCheck.Run(documents, HelpFixtures.Options), problem => problem.Contains("metadata differs"));
        Assert.Contains(HelpCheck.Run(documents, HelpFixtures.Options), problem => problem.Contains("sections or steps"));
    }

    [Fact]
    public void PermissionsAreAnyConditionsAreAllAndDirectLookupsAreFiltered()
    {
        string markdown = HelpFixtures.Markdown().Replace("permissions: []", "permissions: [read, edit]")
            .Replace("conditions: []", "conditions: [cards, credit]");
        HelpCatalogue catalogue = new(HelpFixtures.Documents(markdown), HelpFixtures.Options);
        Assert.NotNull(catalogue.Find("en", "welcome", new(["edit"], ["cards", "credit"])));
        Assert.Null(catalogue.Find("en", "welcome", new(["edit"], ["cards"])));
        Assert.Null(catalogue.Find("en", "welcome", HelpFixtures.Reader));
        Assert.Empty(catalogue.Visible("en", HelpFixtures.Reader));
        Assert.Empty(HelpSearch.Find(catalogue, "en", "cafe", HelpFixtures.Reader));
    }

    [Fact]
    public void EmptyRulesAllowEveryoneAndSearchIgnoresAccentsAndCaseWithCap()
    {
        HelpDocument[] documents = Enumerable.Range(0, 25)
            .SelectMany(index => HelpFixtures.Documents(HelpFixtures.Markdown($"article-{index}"))).ToArray();
        HelpCatalogue catalogue = new(documents, HelpFixtures.Options);
        Assert.NotNull(catalogue.Find("en", "article-0", HelpFixtures.Reader));
        Assert.Equal(20, HelpSearch.Find(catalogue, "en", "CAFE", HelpFixtures.Reader).Count);
        Assert.Empty(HelpSearch.Find(catalogue, "en", "  ", HelpFixtures.Reader));
    }

    [Fact]
    public void ThreeHundredArticlesBuildWithinFiveHundredMilliseconds()
    {
        _ = new HelpCatalogue(HelpFixtures.Documents(HelpFixtures.Markdown()), HelpFixtures.Options);
        HelpDocument[] documents = Enumerable.Range(0, 100)
            .SelectMany(index => HelpFixtures.Documents(HelpFixtures.Markdown($"article-{index}"))).ToArray();
        Stopwatch stopwatch = Stopwatch.StartNew();
        HelpCatalogue catalogue = new(documents, HelpFixtures.Options);
        stopwatch.Stop();
        Assert.Equal(100, catalogue.Visible("en", HelpFixtures.Reader).Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Catalogue took {stopwatch.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void CustomerSignedOutAndSuperUserRulesAreExplicit()
    {
        HelpOptions options = new() { Sections = HelpFixtures.Options.Sections, Customer = true };
        HelpCatalogue privateCatalogue = new(HelpFixtures.Documents(HelpFixtures.Markdown()), options);
        Assert.Null(privateCatalogue.Find("en", "welcome", new([], [], signedIn: false)));
        HelpCatalogue publicCatalogue = new(HelpFixtures.Documents(HelpFixtures.Markdown().Replace("review: ok", "review: ok\nsigned_out: true")), options);
        Assert.NotNull(publicCatalogue.Find("en", "welcome", new([], [], signedIn: false)));
        HelpCatalogue restricted = new(HelpFixtures.Documents(HelpFixtures.Markdown().Replace("permissions: []", "permissions: [edit]")), HelpFixtures.Options);
        Assert.NotNull(restricted.Find("en", "welcome", new([], [], superUser: true)));
    }

    [Fact]
    public void RelatedArticlesAreFilteredAndTenantBrandIsChecked()
    {
        var first = HelpFixtures.Documents(HelpFixtures.Markdown().Replace("related: []", "related: [restricted]"));
        var second = HelpFixtures.Documents(HelpFixtures.Markdown("restricted").Replace("permissions: []", "permissions: [edit]"));
        HelpCatalogue catalogue = new(first.Concat(second), HelpFixtures.Options);
        Assert.Empty(catalogue.Related(catalogue.Find("en", "welcome", HelpFixtures.Reader)!, HelpFixtures.Reader));
        HelpOptions tenantOptions = new() { Sections = HelpFixtures.Options.Sections, TenantBranded = true };
        Assert.NotEmpty(HelpCheck.Run(HelpFixtures.Documents(HelpFixtures.Markdown(body: "Use VIXLY.")), tenantOptions));
        Assert.NotEmpty(HelpCheck.Run(HelpFixtures.Documents(HelpFixtures.Markdown(body: "Use V&#105;xly.")), tenantOptions));
    }

    [Fact]
    public void CodeBracesStayLiteralAndTokensCannotBecomeLinkAddresses()
    {
        string html = new HelpRenderer().Render("`{brandName}` {brandName}", "en", HelpFixtures.Options);
        html = HelpRenderer.ReplaceTokens(html, new Dictionary<string, string> { ["brandName"] = "Company" });
        Assert.Contains("<code>{brandName}</code>", html);
        Assert.Contains("Company", html);
        Assert.Throws<FormatException>(() => new HelpRenderer().Render("[Portal]({portalAddress})", "en", HelpFixtures.Options));
    }

    [Fact]
    public void EmbeddedArticlesAreLoadedFromTheSuppliedAssembly()
    {
        IReadOnlyList<HelpDocument> documents = HelpArticleSource.Read(typeof(HelpFoundationTests).Assembly);
        Assert.Equal(3, documents.Count);
        Assert.Empty(HelpCheck.Run(documents, HelpFixtures.Options));
    }
}
