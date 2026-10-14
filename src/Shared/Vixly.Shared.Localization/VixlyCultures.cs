using System.Globalization;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyCultures.cs
//
// PURPOSE:
// The cultures every V4 host supports (ADR-0005).
//
// RESPONSIBILITIES:
// - List en, fr and es as CultureInfo, en first and default
//
// IMPORTANT NOTES:
// - Neutral cultures: number and date formats follow the
//   browser's regional setting through the request culture
//   provider, the language follows the user (L-4)
// =========================================================
public static class VixlyCultures
{
    public static IReadOnlyList<CultureInfo> Supported { get; } =
        LanguageCode.All.Select(language => CultureInfo.GetCultureInfo(language.Value)).ToArray();

    public static CultureInfo Default { get; } = CultureInfo.GetCultureInfo(LanguageCode.Default.Value);
}
