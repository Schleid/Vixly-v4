using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: UiRulesTests.cs
//
// PURPOSE:
// Enforces the component rules of Global Standard 5.3 on every
// plane Web project (ADR-0001).
//
// RESPONSIBILITIES:
// - UI-6: pages stay under 80 lines, inject no use case or
//   gate, and host no presenter
// - UI-7: markup files stay under 250 lines; presenters take at
//   most 7 dependencies
// - UI-10: every literal text key used in markup exists in the
//   plane resources
//
// IMPORTANT NOTES:
// - Source checks, so the test needs no reference to the Web
//   projects
// - Keys built at run time (status, action names) are covered by
//   the presenter and resource tests, not here
// =========================================================
public sealed class UiRulesTests
{
    private const int PageLineBudget = 80;

    private const int MarkupLineBudget = 250;

    private const int PresenterDependencyBudget = 7;

    private static readonly Regex TextKey = new(@"\bT\[""(?<key>[a-z0-9_.]+)""\]", RegexOptions.CultureInvariant);

    private static readonly Regex Constructor = new(@"public\s+(?<name>\w+Presenter)\s*\((?<parameters>[^)]*)\)", RegexOptions.CultureInvariant);

    public static TheoryData<string> MarkupFiles()
    {
        return Data(Directory.EnumerateFiles(WebRoot(), "*.razor", SearchOption.AllDirectories));
    }

    public static TheoryData<string> PageFiles()
    {
        return Data(Directory.EnumerateFiles(WebRoot(), "*.razor", SearchOption.AllDirectories)
            .Where(path => File.ReadLines(path).Any(line => line.StartsWith("@page ", StringComparison.Ordinal))));
    }

    public static TheoryData<string> PresenterFiles()
    {
        return Data(Directory.EnumerateFiles(WebRoot(), "*Presenter.cs", SearchOption.AllDirectories));
    }

    [Theory]
    [MemberData(nameof(MarkupFiles))]
    public void Markup_StaysWithinItsBudget_AndUsesOnlyKnownTextKeys(string relativePath)
    {
        string[] lines = File.ReadAllLines(FullPath(relativePath));
        HashSet<string> keys = ResourceKeys();

        Assert.True(lines.Length <= MarkupLineBudget, $"{relativePath}: {lines.Length} lines, budget {MarkupLineBudget} (UI-7).");

        foreach (Match match in TextKey.Matches(string.Join('\n', lines)))
        {
            Assert.True(keys.Contains(match.Groups["key"].Value), $"{relativePath}: text key '{match.Groups["key"].Value}' is not in PlatformText.resx (UI-10).");
        }
    }

    [Theory]
    [MemberData(nameof(PageFiles))]
    public void Page_IsRouteAndCompositionOnly(string relativePath)
    {
        string[] lines = File.ReadAllLines(FullPath(relativePath));

        Assert.True(lines.Length <= PageLineBudget, $"{relativePath}: {lines.Length} lines, budget {PageLineBudget} (UI-6).");
        Assert.DoesNotContain(lines, line => line.StartsWith("@inject", StringComparison.Ordinal)
            && (line.Contains("UseCase", StringComparison.Ordinal) || line.Contains("Gate", StringComparison.Ordinal) || line.Contains("CurrentUser", StringComparison.Ordinal)));
        Assert.DoesNotContain(lines, line => line.Contains("PresenterComponent", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(PresenterFiles))]
    public void Presenter_TakesAtMostSevenDependencies(string relativePath)
    {
        foreach (Match match in Constructor.Matches(File.ReadAllText(FullPath(relativePath))))
        {
            int count = match.Groups["parameters"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

            Assert.True(count <= PresenterDependencyBudget, $"{relativePath}: {match.Groups["name"].Value} takes {count} dependencies, budget {PresenterDependencyBudget} (UI-7).");
        }
    }

    private static TheoryData<string> Data(IEnumerable<string> paths)
    {
        TheoryData<string> data = new();

        foreach (string path in paths.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            data.Add(Path.GetRelativePath(RepositoryLocator.Root, path));
        }

        return data;
    }

    private static string WebRoot()
    {
        return Path.Combine(RepositoryLocator.Root, "src", "Platform", "Vixly.Platform.Web");
    }

    private static HashSet<string> ResourceKeys()
    {
        string path = Path.Combine(RepositoryLocator.Root, "src", "Platform", "Vixly.Platform.Resources", "PlatformText.resx");

        return XDocument.Load(path)
            .Descendants("data")
            .Select(element => (string?)element.Attribute("name") ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string FullPath(string relativePath)
    {
        return Path.Combine(RepositoryLocator.Root, relativePath);
    }
}
