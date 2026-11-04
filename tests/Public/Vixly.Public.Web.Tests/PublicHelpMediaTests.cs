using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vixly.Public.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicHelpMediaTests.cs
//
// PURPOSE:
// Keeps annotated help images complete, localized and reproducible.
//
// RESPONSIBILITIES:
// - Verify real image files, capture hashes and article references.
// - Verify anonymous delivery through the application pipeline.
//
// IMPORTANT NOTES:
// - Screenshot capture is separate from workflow verification.
// - These tests never connect to a payment or email service.
// =========================================================
public sealed class PublicHelpMediaTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public void EveryArticleHasLocalizedImagesWithCaptureProvenance(string language)
    {
        string root = Root();
        string folder = Path.Combine(root, "src/Public/Vixly.Public.Web/wwwroot/help/media/v2");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")));
        Assert.Equal("headless browser document only", manifest.RootElement.GetProperty("capture").GetString());
        JsonElement[] captures = manifest.RootElement.GetProperty("images").EnumerateArray()
            .Where(item => item.GetProperty("language").GetString() == language).ToArray();
        Assert.Equal(14, captures.Length);
        PublicHelpCatalogue help = new();
        foreach (HelpArticle article in help.Catalogue.Visible(language, PublicHelpCatalogue.Reader))
        {
            MatchCollection images = Regex.Matches(article.Html, "<img\\b[^>]*>");
            Assert.NotEmpty(images.Cast<Match>());
            foreach (Match image in images)
            {
                string address = Regex.Match(image.Value, "\\bsrc=\"([^\"]+)\"").Groups[1].Value;
                string alt = Regex.Match(image.Value, "\\balt=\"([^\"]+)\"").Groups[1].Value;
                Assert.StartsWith("/help/media/v2/" + language + "-", address, StringComparison.Ordinal);
                Assert.True(alt.Length > 30, article.Metadata.Slug);
                JsonElement capture = Assert.Single(captures, item => item.GetProperty("file").GetString() == Path.GetFileName(address));
                Assert.NotEmpty(capture.GetProperty("marked").EnumerateArray());
                byte[] bytes = File.ReadAllBytes(Path.Combine(folder, Path.GetFileName(address)));
                Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
                Assert.Equal(capture.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
                Assert.Contains("href=\"" + address + "\"", article.Html, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task ScreenshotAssetsAreDeliveredAnonymously(string language)
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        using HttpClient client = host.Client();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json")));
        foreach (JsonElement capture in manifest.RootElement.GetProperty("images").EnumerateArray()
            .Where(item => item.GetProperty("language").GetString() == language))
        {
            using HttpResponseMessage response = await client.GetAsync("/help/media/v2/" + capture.GetProperty("file").GetString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Content.Headers.ContentType?.MediaType == "image/png",
                $"{capture.GetProperty("file").GetString()}: type={response.Content.Headers.ContentType}; length={(await response.Content.ReadAsByteArrayAsync()).Length}; headers={response.Headers}");
        }
    }

    private static string Root()
    {
        DirectoryInfo? folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "AGENTS.md"))) folder = folder.Parent;
        return folder?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
