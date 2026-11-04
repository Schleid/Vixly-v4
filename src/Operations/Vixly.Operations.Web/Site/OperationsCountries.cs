using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Resources;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsCountries.cs
//
// PURPOSE:
// Country names and flags for the tenant pages.
//
// RESPONSIBILITIES:
// - Name a country in the current language
// - Make its flag from the two regional-indicator letters
// - List countries for a select box
//
// IMPORTANT NOTES:
// - Names come from OperationsText (ui.country.XX)
// =========================================================
public static class OperationsCountries
{
    public static string Name(IStringLocalizer<OperationsText> text, string code)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text["ui.country." + code].Value;
    }

    public static string Flag(string code)
    {
        return code is { Length: 2 } && char.IsAsciiLetterUpper(code[0]) && char.IsAsciiLetterUpper(code[1])
            ? char.ConvertFromUtf32(0x1F1E6 + code[0] - 'A') + char.ConvertFromUtf32(0x1F1E6 + code[1] - 'A')
            : string.Empty;
    }

    public static IReadOnlyList<(string Code, string Name)> Options(IStringLocalizer<OperationsText> text)
    {
        return OperationsCountryCodes.All
            .Select(code => (code, Name(text, code)))
            .OrderBy(country => country.Item2, StringComparer.Create(CultureInfo.CurrentUICulture, true))
            .ToList();
    }
}
