namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpCheck.cs
//
// PURPOSE:
// Implements shared help rendering, lookup or publication checks.
//
// RESPONSIBILITIES:
// - Enforce the help foundation contract of ADR-0023.
//
// IMPORTANT NOTES:
// - Applications supply trusted reader rules and validation catalogues.
// =========================================================
public static class HelpCheck
{
    public static IReadOnlyList<string> Run(IEnumerable<HelpDocument> documents, HelpOptions options)
    {
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(options);
        List<string> problems = [];
        List<(string Language, HelpFrontMatter Metadata, string Body)> articles = [];
        foreach (HelpDocument document in documents)
        {
            try
            {
                if (document.Language is not ("en" or "fr" or "es")) throw new FormatException("Unknown language.");
                (HelpFrontMatter metadata, string body) = HelpFrontMatter.Parse(document.Markdown, document.Language, options);
                articles.Add((document.Language, metadata, body));
                foreach (string problem in HelpRenderer.Inspect(body, options)) problems.Add($"{document.Path}: {problem}");
                if (metadata.Permissions.Any(code => !options.Permissions.Contains(code))) problems.Add($"{document.Path}: Unknown permission.");
                if (metadata.Conditions.Any(code => !options.Conditions.Contains(code))) problems.Add($"{document.Path}: Unknown condition.");
                if (metadata.Routes.Any(route => !options.Routes.Contains(route))) problems.Add($"{document.Path}: Unknown route.");
                if (options.TenantBranded && (metadata.Title.Contains("Vixly", StringComparison.OrdinalIgnoreCase)
                    || metadata.Summary.Contains("Vixly", StringComparison.OrdinalIgnoreCase)
                    || HelpRenderer.PlainText(body).Contains("Vixly", StringComparison.OrdinalIgnoreCase))) problems.Add($"{document.Path}: Tenant help contains Vixly.");
            }
            catch (FormatException exception)
            {
                problems.Add($"{document.Path}: {exception.Message}");
            }
        }
        foreach (var group in articles.GroupBy(article => article.Metadata.Slug))
        {
            if (group.Count() != 3 || !group.Select(article => article.Language).ToHashSet().SetEquals(["en", "fr", "es"]))
                problems.Add($"{group.Key}: Duplicate slug or missing language.");
            var english = group.FirstOrDefault(article => article.Language == "en");
            if (english.Metadata is null) continue;
            foreach (var translation in group.Where(article => article.Language != "en"))
            {
                HelpFrontMatter a = english.Metadata;
                HelpFrontMatter b = translation.Metadata;
                if (a.Section != b.Section || a.Order != b.Order || a.SignedOut != b.SignedOut || a.Updated != b.Updated
                    || !a.Permissions.SequenceEqual(b.Permissions) || !a.Conditions.SequenceEqual(b.Conditions)
                    || !a.Routes.SequenceEqual(b.Routes) || !a.Related.SequenceEqual(b.Related) || !a.Sources.SequenceEqual(b.Sources))
                    problems.Add($"{group.Key}: Translated metadata differs.");
                if (HelpRenderer.Structure(english.Body) != HelpRenderer.Structure(translation.Body))
                    problems.Add($"{group.Key}: Translated sections or steps differ.");
            }
        }
        foreach (var language in articles.GroupBy(article => article.Language))
        {
            HashSet<string> slugs = language.Select(article => article.Metadata.Slug).ToHashSet(StringComparer.Ordinal);
            foreach (var article in language)
                if (article.Metadata.Related.Concat(HelpRenderer.LinkedSlugs(article.Body)).Any(slug => !slugs.Contains(slug)))
                    problems.Add($"{article.Metadata.Slug}: Missing related or linked article.");
            foreach (var route in language.SelectMany(article => article.Metadata.Routes).GroupBy(route => route))
                if (route.Count() > 1) problems.Add($"{route.Key}: Route claimed twice.");
        }
        return problems.AsReadOnly();
    }
}
