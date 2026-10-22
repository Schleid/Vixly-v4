using System.Globalization;

namespace Vixly.Public.Web.Features.Plans;

// =========================================================
// FILE: PlanDisplay.cs
//
// PURPOSE:
// Formats plan prices and filters feature codes for the public pages.
//
// RESPONSIBILITIES:
// - Format money in the page culture with its ISO code
// - Know which feature codes have text
//
// IMPORTANT NOTES:
// - Currency is never inferred from the language (L-8)
// =========================================================
public static class PlanDisplay
{
    public static IReadOnlyList<string> Features { get; } = new[]
    {
        "homepage_content_customization", "portal_theme_customization", "custom_domain", "white_label_portal", "advanced_analytics", "data_export", "partner_agents"
    };

    public static bool Known(string code) => Features.Contains(code, StringComparer.Ordinal);

    public static string Money(decimal amount, string currencyCode)
    {
        return amount.ToString("N2", CultureInfo.CurrentCulture) + " " + currencyCode;
    }
}
