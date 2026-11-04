using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Operations.Web.Features.Help;
using Vixly.Shared.Help;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpContentTests.cs
// PURPOSE:
// Checks localized published instructions against their sources.
// RESPONSIBILITIES:
// - Verify exact labels, source paths, length and company branding.
// IMPORTANT NOTES:
// - Translation review is still a human publication task.
// =========================================================
public sealed class OperationsHelpContentTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public void ArticlesUseRealLabelsAndSources(string language)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string root = directory.FullName;
        string suffix = language == "en" ? "" : "." + language;
        HashSet<string> labels = XDocument.Load(Path.Combine(root, $"src/Operations/Vixly.Operations.Resources/OperationsText{suffix}.resx"))
            .Descendants("data").Select(element => element.Element("value")?.Value ?? "").ToHashSet(StringComparer.Ordinal);
        OperationsHelpCatalogue help = OperationsHelpTestCatalogue.Create();
        IReadOnlyList<HelpArticle> articles = help.Catalogue.Visible(language, new(OperationsPermissions.All, []));
        // Every article file is published; none is hidden by the catalogue.
        int files = Directory.GetFiles(Path.Combine(root, "src/Operations/Vixly.Operations.Resources/Help", language), "*.md", SearchOption.AllDirectories).Length;
        Assert.Equal(files, articles.Count);
        foreach (HelpArticle article in articles)
        {
            Assert.Contains("{brandName}", article.Body);
            Assert.DoesNotContain("Vixly", article.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("![", article.Body);
            Assert.InRange(article.Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length, 150, 600);
            foreach (Match match in Regex.Matches(article.Body, @"\*\*(.*?)\*\*"))
                Assert.Contains(match.Groups[1].Value, labels);
            foreach (string source in article.Metadata.Sources) Assert.True(File.Exists(Path.Combine(root, source)), source);
        }
    }
}
