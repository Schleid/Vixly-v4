using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Customer.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerHelpContentTests.cs
// PURPOSE:
// Verifies localized instructions and their real-page image provenance.
// RESPONSIBILITIES:
// - Check exact labels, private visibility and artifact integrity.
// IMPORTANT NOTES:
// - Human translations and live workflows need separate review.
// =========================================================
public sealed class CustomerHelpContentTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public void GuidesUseRealLabelsAndLocalizedVerifiedImages(string language)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string root = directory.FullName;
        string suffix = language == "en" ? "" : "." + language;
        HashSet<string> labels = XDocument.Load(Path.Combine(root, $"src/Customer/Vixly.Customer.Resources/CustomerText{suffix}.resx"))
            .Descendants("data").Select(item => item.Element("value")?.Value ?? "").ToHashSet();
        CustomerHelpCatalogue help = new();
        HelpReader reader = new([], []);
        var articles = help.Catalogue.Visible(language, reader);
        Assert.Equal(3, articles.Count);
        Assert.Empty(help.Catalogue.Visible(language, new([], [], signedIn: false)));
        string media = Path.Combine(root, "src/Customer/Vixly.Customer.Web/wwwroot/help/media/v2");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(media, "manifest.json")));
        foreach (HelpArticle article in articles)
        {
            Assert.Contains("{brandName}", article.Body);
            Assert.DoesNotContain("Vixly", article.Body, StringComparison.OrdinalIgnoreCase);
            Assert.InRange(article.Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length, 150, 600);
            foreach (Match match in Regex.Matches(article.Body, @"\*\*(.*?)\*\*")) Assert.Contains(match.Groups[1].Value, labels);
            foreach (string source in article.Metadata.Sources) Assert.True(File.Exists(Path.Combine(root, source)), source);
            Assert.Contains("/customer/help/media/v2/" + language + "-", article.Html);
            Assert.Contains("alt=", article.Html);
            foreach (string route in article.Metadata.Routes)
                Assert.Equal("/customer/help/" + article.Metadata.Slug, help.ContextAddress(language, route, true));
        }
        var images = manifest.RootElement.GetProperty("images").EnumerateArray().Where(item => item.GetProperty("language").GetString() == language).ToArray();
        Assert.Equal(3, images.Length);
        foreach (JsonElement item in images)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(media, item.GetProperty("file").GetString()!));
            Assert.Equal("89504E470D0A1A0A", Convert.ToHexString(bytes.AsSpan(0, 8)));
            Assert.Equal(item.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }
    }
}
