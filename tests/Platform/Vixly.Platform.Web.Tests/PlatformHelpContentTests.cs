using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Platform.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformHelpContentTests.cs
//
// PURPOSE:
// Checks article labels, length and source provenance.
//
// RESPONSIBILITIES:
// - Compare every bold UI label with its language's resources.
//
// IMPORTANT NOTES:
// - Does not replace workflow walkthrough or native review.
// =========================================================
public sealed class PlatformHelpContentTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    public void LabelsAndSourcesMatchTheCurrentApplication(string language)
    {
        string root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AGENTS.md"))) root = Directory.GetParent(root)!.FullName;
        string suffix = language == "en" ? string.Empty : "." + language;
        HashSet<string> labels = XDocument.Load(Path.Combine(root, "src/Platform/Vixly.Platform.Resources/PlatformText" + suffix + ".resx"))
            .Descendants("data").Select(node => node.Element("value")!.Value).ToHashSet(StringComparer.Ordinal);
        PlatformHelpCatalogue help = new();
        foreach (HelpArticle article in help.Catalogue.Visible(language, new([], [], superUser: true)))
        {
            string body = article.Body;
            int words = Regex.Matches(body, @"\S+").Count;
            Assert.True(words is >= 150 and <= 600, $"{article.SourcePath}: {words} words");
            foreach (Match match in Regex.Matches(body, @"\*\*([^*]+)\*\*"))
                Assert.Contains(match.Groups[1].Value, labels);
            foreach (string source in article.Metadata.Sources)
                Assert.True(File.Exists(Path.Combine(root, source)), source);
            Assert.DoesNotContain("[[", body, StringComparison.Ordinal);
        }
    }
}
