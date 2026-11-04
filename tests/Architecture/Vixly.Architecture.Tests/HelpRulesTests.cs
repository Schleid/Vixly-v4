using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Shared.Help;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: HelpRulesTests.cs
//
// PURPOSE:
// Verifies the shared help foundation.
//
// RESPONSIBILITIES:
// - Enforce the help contract of ADR-0023.
//
// IMPORTANT NOTES:
// - No live application or customer data is required.
// =========================================================
public sealed class HelpRulesTests
{
    [Fact]
    public void SharedHelpReferencesOnlyMarkdigAndBcl()
    {
        string path = Path.Combine(RepositoryLocator.Root, "src/Shared/Vixly.Shared.Help/Vixly.Shared.Help.csproj");
        XDocument project = XDocument.Load(path);
        Assert.Empty(project.Descendants("ProjectReference"));
        Assert.Empty(project.Descendants("FrameworkReference"));
        Assert.Equal(new[] { "Markdig" }, project.Descendants("PackageReference").Select(element => (string?)element.Attribute("Include")));
    }

    [Theory]
    [InlineData("Public", "Web", "1-PUBLIC")]
    [InlineData("Platform", "Resources", "2-CONTROL")]
    [InlineData("Customer", "Resources", "3-CUSTOMER")]
    [InlineData("Operations", "Resources", "4-OPERATIONS")]
    public void HelpFoldersPassPublicationChecks(string plane, string layer, string stage)
    {
        string folder = Path.Combine(RepositoryLocator.Root, $"src/{plane}/Vixly.{plane}.{layer}/Help");
        if (!Directory.Exists(folder)) return;
        HelpDocument[] documents = Directory.EnumerateFiles(folder, "*.md", SearchOption.AllDirectories)
            .Select(path => new HelpDocument(Path.GetRelativePath(folder, path).Split(Path.DirectorySeparatorChar)[0], path, File.ReadAllText(path))).ToArray();
        string stageText = File.ReadAllText(Path.Combine(RepositoryLocator.Root, $"docs/help/HELP-{stage}.md"));
        string sectionList = stageText.Split('\n').Single(line => line.StartsWith("Sections:", StringComparison.Ordinal));
        HashSet<string> sections = Regex.Matches(sectionList, @"`([a-z]+(?:-[a-z]+)*)`")
            .Select(match => match.Groups[1].Value).ToHashSet();
        string planeRoot = Path.Combine(RepositoryLocator.Root, "src", plane);
        string[] sourceFiles = Directory.EnumerateFiles(planeRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(planeRoot, "*.razor", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")).ToArray();
        string source = string.Join('\n', sourceFiles.Select(File.ReadAllText));
        string permissionSource = plane switch
        {
            "Platform" => File.ReadAllText(Path.Combine(planeRoot, "Vixly.Platform.Application/Security/PlatformPermissions.cs")),
            "Operations" => File.ReadAllText(Path.Combine(planeRoot, "Vixly.Operations.Domain/Access/OperationsPermissions.cs")),
            _ => string.Empty
        };
        HashSet<string> permissions = Regex.Matches(permissionSource, "\"((?:ops|platform)\\.[a-z0-9_.]+)\"")
            .Select(match => match.Groups[1].Value).ToHashSet();
        if (plane == "Platform") permissions.Add("superuser");
        // Source-level route inventory; application HTTP tests additionally check EndpointDataSource.
        HashSet<string> routes = Regex.Matches(source, "\"(/[^\"\\r\\n]*)\"")
            .Select(match => match.Groups[1].Value).ToHashSet();
        string[] prefixes = routes.ToArray();
        foreach (string prefix in prefixes)
            foreach (string suffix in prefixes) routes.Add(prefix.TrimEnd('/') + suffix);
        HashSet<string> conditions = plane switch
        {
            "Customer" => new() { "card_payments", "text_notices", "credit_account", "insurance" },
            "Operations" => new() { "partner_agents", "text_messages", "tenant_api", "website_theme", "website_content" },
            _ => []
        };
        HashSet<string> tokens = plane switch
        {
            "Customer" => new() { "brandName" },
            "Operations" => new() { "brandName", "portalAddress" },
            _ => []
        };
        HelpOptions options = new()
        {
            Sections = sections, Permissions = permissions, Conditions = conditions, Routes = routes,
            Tokens = tokens, Customer = plane == "Customer", TenantBranded = plane is "Customer" or "Operations",
            MediaPath = plane == "Customer" ? "/customer/help/media/" : "/help/media/"
        };
        Assert.Empty(HelpCheck.Run(documents, options));
    }
}
