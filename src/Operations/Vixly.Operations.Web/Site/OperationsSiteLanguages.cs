using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSiteLanguages.cs
//
// PURPOSE:
// The languages of a tenant site and their addresses (Slice 1B).
//
// RESPONSIBILITIES:
// - List the site languages
// - Read the language of a site address
//
// IMPORTANT NOTES:
// - Every V4 language (en, fr, es); per-tenant choice arrives with tenant settings (1D)
// =========================================================
public static class OperationsSiteLanguages
{
    public static IReadOnlyList<string> All { get; } = VixlyCultures.Supported.Select(culture => culture.TwoLetterISOLanguageName).ToList();

    public static bool IsSiteLanguage(string? value) => value is not null && All.Contains(value, StringComparer.Ordinal);

    public static string? FromPath(PathString path)
    {
        string value = path.Value ?? string.Empty;

        if (value.Length < 3 || value[0] != '/')
        {
            return null;
        }

        string first = value[1..].Split('/')[0];
        return IsSiteLanguage(first) ? first : null;
    }
}
