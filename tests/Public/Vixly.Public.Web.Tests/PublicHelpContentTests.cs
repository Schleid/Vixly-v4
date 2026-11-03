using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Public.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicHelpContentTests.cs
//
// PURPOSE:
// Keeps article labels aligned with the application's resources.
//
// RESPONSIBILITIES:
// - Verify exact localized labels, article length and source paths.
//
// IMPORTANT NOTES:
// - Bold text names actual UI labels, never translated guesses.
// - These checks complement the shared publication validator.
// =========================================================
public sealed class PublicHelpContentTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public void ArticleLabelsMatchResourcesAndSourcesExist(string language)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string root = directory.FullName;
        string suffix = language == "en" ? string.Empty : "." + language;
        HashSet<string> labels = XDocument.Load(Path.Combine(root, $"src/Public/Vixly.Public.Web/Resources/PublicText{suffix}.resx"))
            .Descendants("data").Select(element => element.Element("value")?.Value ?? string.Empty).ToHashSet(StringComparer.Ordinal);
        PublicHelpCatalogue help = new();
        foreach (HelpArticle article in help.Catalogue.Visible(language, PublicHelpCatalogue.Reader))
        {
            foreach (Match match in Regex.Matches(article.Body, @"\*\*(.*?)\*\*"))
                Assert.True(labels.Contains(match.Groups[1].Value), $"{language}/{article.Metadata.Slug}: unknown label {match.Groups[1].Value}");
            int words = article.Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.InRange(words, 150, 600);
            foreach (string source in article.Metadata.Sources) Assert.True(File.Exists(Path.Combine(root, source)), source);
        }
    }
}
