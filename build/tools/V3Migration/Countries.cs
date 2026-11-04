using System.Globalization;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Tools.V3Migration;

// =========================================================
// FILE: Countries.cs
//
// PURPOSE:
// Reads a V3 country, typed as a code or a name, as a V4 country code (owner, 2026-10-26).
//
// RESPONSIBILITIES:
// - Accept a two-letter code, a three-letter code, or the English or local name
//
// IMPORTANT NOTES:
// - Only codes Operations accepts; anything else is null and the report says so
// =========================================================
internal static class Countries
{
    private static readonly Dictionary<string, string> Names = Build();

    public static string? Code(string? value)
    {
        string text = Fold(value);
        return text.Length == 0 ? null : Names.TryGetValue(text, out string? code) ? code : null;
    }

    private static Dictionary<string, string> Build()
    {
        Dictionary<string, string> names = new(StringComparer.Ordinal);

        foreach (string code in OperationsCountryCodes.All)
        {
            names[code] = code;

            try
            {
                RegionInfo region = new(code);
                names.TryAdd(Fold(region.ThreeLetterISORegionName), code);
                names.TryAdd(Fold(region.EnglishName), code);
                names.TryAdd(Fold(region.NativeName), code);
            }
            catch (ArgumentException)
            {
                // A code .NET does not know keeps only its own two letters.
            }
        }

        names.TryAdd("USA", "US");
        names.TryAdd("UNITED STATES OF AMERICA", "US");
        names.TryAdd("ETATS UNIS", "US");
        names.TryAdd("DOMINICAN REPUBLIC", "DO");
        names.TryAdd("REPUBLIQUE DOMINICAINE", "DO");
        names.TryAdd("UK", "GB");
        return names;
    }

    // Upper case, without accents, dashes or dots: "Haïti" and "HAITI" read the same.
    private static string Fold(string? value)
    {
        string text = (value ?? string.Empty).Trim().Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder folded = new();

        foreach (char letter in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letter) == UnicodeCategory.NonSpacingMark || letter is '.')
            {
                continue;
            }

            folded.Append(letter is '-' ? ' ' : char.ToUpperInvariant(letter));
        }

        return string.Join(' ', folded.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
