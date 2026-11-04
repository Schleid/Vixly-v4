using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSiteLooks.cs
//
// PURPOSE:
// The address names of the website themes and the files and families of the curated fonts (website themes, step 1).
//
// RESPONSIBILITIES:
// - Turn a theme into its name in the preview address and back
// - Give the CSS family and the file of a font
//
// IMPORTANT NOTES:
// - Fonts are self-hosted under wwwroot/fonts/site (README.md there); the default font is the product font
// - Unknown names give null: the preview then shows the draft's own theme
// =========================================================
public static class OperationsSiteLooks
{
    public static string Slug(OperationsSiteTheme theme) => theme switch
    {
        OperationsSiteTheme.Horizon => "horizon",
        OperationsSiteTheme.AirSea => "airsea",
        _ => "classic"
    };

    public static OperationsSiteTheme? Theme(string? slug) => slug?.Trim().ToLowerInvariant() switch
    {
        "classic" => OperationsSiteTheme.Classic,
        "horizon" => OperationsSiteTheme.Horizon,
        "airsea" => OperationsSiteTheme.AirSea,
        _ => null
    };

    public static string Slug(OperationsSiteFont font) => font switch
    {
        OperationsSiteFont.Manrope => "manrope",
        OperationsSiteFont.PlusJakartaSans => "plus-jakarta-sans",
        OperationsSiteFont.DmSans => "dm-sans",
        OperationsSiteFont.Lora => "lora",
        _ => "default"
    };

    // The font file to preload; none for the default font (Shared.UI loads it).
    public static string? File(OperationsSiteFont font) => font == OperationsSiteFont.Default ? null : "/fonts/site/" + Slug(font) + ".woff2";
}
