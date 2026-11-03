using System.Globalization;
using System.Text.RegularExpressions;
namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpFrontMatter.cs
//
// PURPOSE:
// Parses the deliberately limited article metadata format.
//
// RESPONSIBILITIES:
// - Implement the shared help foundation of ADR-0023.
//
// IMPORTANT NOTES:
// - Contains no plane dependencies or request identity.
// =========================================================
public sealed record HelpFrontMatter(
    string Slug, string Title, string Summary, string Section, int Order,
    IReadOnlyList<string> Permissions, IReadOnlyList<string> Conditions,
    IReadOnlyList<string> Routes, IReadOnlyList<string> Related,
    IReadOnlyList<string> Sources, DateOnly Updated, string Review, bool SignedOut)
{
    public static (HelpFrontMatter Metadata, string Body) Parse(string markdown, string language, HelpOptions options)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
        if (lines[0] != "---") throw new FormatException("Missing front matter.");
        int end = Array.IndexOf(lines, "---", 1);
        if (end < 0) throw new FormatException("Unclosed front matter.");
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        HashSet<string> blockLists = [];
        string? listKey = null;
        for (int i = 1; i < end; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("  - ", StringComparison.Ordinal))
            {
                if (listKey is null) throw new FormatException("Unexpected list item.");
                fields[listKey] += (fields[listKey].Length == 0 ? "" : "\n") + line[4..].Trim();
                continue;
            }
            int separator = line.IndexOf(':');
            if (separator <= 0) throw new FormatException("Invalid front matter line.");
            string key = line[..separator];
            string value = line[(separator + 1)..].Trim();
            if (!AllowedFields.Contains(key) || !fields.TryAdd(key, value))
                throw new FormatException($"Unknown or duplicate field: {key}.");
            listKey = value.Length == 0 ? key : null;
            if (listKey is not null) blockLists.Add(listKey);
        }
        string Required(string key)
        {
            if (!fields.TryGetValue(key, out string? value)) throw new FormatException($"Missing field: {key}.");
            return value;
        }
        string Text(string key, int max)
        {
            string value = Required(key);
            if (string.IsNullOrWhiteSpace(value) || value.Length > max) throw new FormatException($"Invalid {key}.");
            return value;
        }
        IReadOnlyList<string> List(string key)
        {
            string value = Required(key);
            string[] items;
            if (value.StartsWith('[') && value.EndsWith(']'))
                items = value[1..^1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            else if (blockLists.Contains(key))
                items = value.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            else throw new FormatException($"Invalid list: {key}.");
            if (items.Distinct(StringComparer.Ordinal).Count() != items.Length)
                throw new FormatException($"Duplicate list item: {key}.");
            return Array.AsReadOnly(items);
        }
        string slug = Text("slug", 60);
        if (!SlugPattern.IsMatch(slug)) throw new FormatException("Invalid slug.");
        string section = Text("section", 60);
        if (!options.Sections.Contains(section)) throw new FormatException("Unknown section.");
        if (!int.TryParse(Required("order"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int order))
            throw new FormatException("Invalid order.");
        if (!DateOnly.TryParseExact(Required("updated"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly updated))
            throw new FormatException("Invalid updated date.");
        string review = Required("review");
        if (review is not ("ok" or "needs-review") || (language == "en" && review != "ok"))
            throw new FormatException("Invalid review.");
        bool signedOut = false;
        if (fields.TryGetValue("signed_out", out string? signed))
        {
            if (!options.Customer || signed is not ("true" or "false")) throw new FormatException("Invalid signed_out.");
            signedOut = signed == "true";
        }
        IReadOnlyList<string> related = List("related");
        IReadOnlyList<string> sources = List("sources");
        if (related.Count > 5 || sources.Count == 0) throw new FormatException("Invalid related or sources.");
        if (sources.Any(path => path.StartsWith('/') || path.Contains('\\') || path.Split('/').Contains("..")))
            throw new FormatException("Sources must be repository-relative paths.");
        HelpFrontMatter metadata = new(slug, Text("title", 70), Text("summary", 160), section, order,
            List("permissions"), List("conditions"), List("routes"), related, sources, updated, review, signedOut);
        return (metadata, string.Join('\n', lines[(end + 1)..]));
    }

    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "slug", "title", "summary", "section", "order", "permissions", "conditions",
        "routes", "related", "sources", "updated", "review", "signed_out"
    };
}
