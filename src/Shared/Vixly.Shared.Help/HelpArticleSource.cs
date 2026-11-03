using System.Reflection;
using System.Text.RegularExpressions;
namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpArticleSource.cs
//
// PURPOSE:
// Reads embedded article resources without filesystem access.
//
// RESPONSIBILITIES:
// - Implement the shared help foundation of ADR-0023.
//
// IMPORTANT NOTES:
// - Contains no plane dependencies or request identity.
// =========================================================
public static class HelpArticleSource
{
    public static IReadOnlyList<HelpDocument> Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        List<HelpDocument> documents = [];
        foreach (string name in assembly.GetManifestResourceNames().Order(StringComparer.Ordinal))
        {
            Match match = Regex.Match(name, @"^.*\.Help\.([^.]+)\..+\.md$", RegexOptions.CultureInvariant);
            if (!match.Success) continue;
            using Stream stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("Embedded article unavailable.");
            using StreamReader reader = new(stream);
            documents.Add(new(match.Groups[1].Value, name, reader.ReadToEnd()));
        }
        return documents.AsReadOnly();
    }
}
