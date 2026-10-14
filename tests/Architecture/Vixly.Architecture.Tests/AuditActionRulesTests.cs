using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Platform.Application.Audit;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: AuditActionRulesTests.cs
//
// PURPOSE:
// Keeps the audit action catalogue complete, so the audit log
// can filter by every action and name it in every language.
//
// RESPONSIBILITIES:
// - Every action code a Platform use case writes is in
//   PlatformAuditActions
// - Every catalogued code has text (ui.audit.action.<code>) in
//   en, fr and es
//
// IMPORTANT NOTES:
// - Reads the source: an action code is the first platform.*
//   literal after Record(, RecordChange(, RecordMove(,
//   RecordFailedAttemptAsync( or new PlatformUserLifecycleAction(
// =========================================================
public sealed class AuditActionRulesTests
{
    private static readonly Regex Written = new(
        @"(?:\.Record\(|\.RecordChange\(|\.RecordMove\(|RecordFailedAttemptAsync\(|new PlatformUserLifecycleAction\()[^;]*?""(?<code>platform\.[a-z0-9_.]+)""",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    [Fact]
    public void EveryWrittenActionCode_IsCatalogued()
    {
        string folder = Path.Combine(RepositoryLocator.Root, "src", "Platform", "Vixly.Platform.Application");
        List<string> written = RepositoryLocator.CSharpFiles()
            .Where(path => path.StartsWith(folder, StringComparison.Ordinal))
            .SelectMany(path => Written.Matches(File.ReadAllText(path)).Select(match => match.Groups["code"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(written.Count > 30, "The scan is not reaching the audit calls.");
        Assert.All(written, code => Assert.True(PlatformAuditActions.IsKnown(code), $"{code} is written but not in PlatformAuditActions."));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".fr")]
    [InlineData(".es")]
    public void EveryCataloguedAction_HasText(string language)
    {
        string path = Path.Combine(RepositoryLocator.Root, "src", "Platform", "Vixly.Platform.Resources", "PlatformText" + language + ".resx");
        HashSet<string> keys = XDocument.Load(path)
            .Descendants("data")
            .Select(data => (string)data.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(PlatformAuditActions.Catalogue, code => Assert.Contains("ui.audit.action." + code, keys));
    }
}
