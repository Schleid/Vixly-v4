namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicSiteOptions.cs
//
// PURPOSE:
// Settings of the public site (Stage 2).
//
// RESPONSIBILITIES:
// - Platform.Api address, canonical address, sign-up switch, bot check, caller key, checkout origins
//
// IMPORTANT NOTES:
// - Section PublicSite
// - CallerPrivateKey is a secret: environment variable only (S-11)
// - RegistrationOpen stays false until Operations can take new owners (owner decision D-U4)
// =========================================================
public sealed class PublicSiteOptions
{
    public const string SectionName = "PublicSite";

    public string PlatformApiBaseUrl { get; set; } = string.Empty;

    public string CanonicalBaseUrl { get; set; } = string.Empty;

    public bool RegistrationOpen { get; set; }

    public string? TurnstileSiteKey { get; set; }

    public string? CallerKeyId { get; set; }

    public string? CallerPrivateKey { get; set; }

    public string[] CheckoutOrigins { get; set; } = Array.Empty<string>();

    public string? KeysPath { get; set; }

    public string SalesEmail { get; set; } = string.Empty;

    public bool BotCheckOn => !string.IsNullOrWhiteSpace(TurnstileSiteKey);
}
