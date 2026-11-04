using System.Globalization;

namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpSearch.cs
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
public static class HelpSearch
{
    public static IReadOnlyList<HelpArticle> Find(HelpCatalogue catalogue, string language, string query, HelpReader reader)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        if (string.IsNullOrWhiteSpace(query)) return [];
        string term = query.Trim();
        CompareInfo comparison = CultureInfo.GetCultureInfo(language).CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        bool Matches(string value) => comparison.IndexOf(value, term, options) >= 0;
        return catalogue.Visible(language, reader)
            .Where(article => Matches(article.Metadata.Title) || Matches(article.Metadata.Summary) || Matches(article.PlainText))
            .OrderByDescending(article => Matches(article.Metadata.Title)).Take(20).ToArray();
    }
}
