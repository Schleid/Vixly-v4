namespace Vixly.Platform.Infrastructure.Tenants;

// =========================================================
// FILE: PlatformBotCheckOptions.cs
//
// PURPOSE:
// Configuration of the public bot check (section PlatformBotCheck,
// D-T12).
//
// RESPONSIBILITIES:
// - Turn the check on and hold the Turnstile secret
//
// IMPORTANT NOTES:
// - The secret comes from user secrets or the host, never the
//   repository (S-11)
// - Disabled means every request passes: development only
// =========================================================
public sealed class PlatformBotCheckOptions
{
    public const string SectionName = "PlatformBotCheck";

    public bool Enabled { get; set; } = true;

    public string SecretKey { get; set; } = string.Empty;

    public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
