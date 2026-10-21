namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerBotCheckOptions.cs
//
// PURPOSE:
// Settings of the bot check on customer sign-up (ADR-0019, Slice 2).
//
// RESPONSIBILITIES:
// - Turn the check on; hold the Turnstile keys
//
// IMPORTANT NOTES:
// - The secret comes from user secrets or the host, never from a committed file
// =========================================================
public sealed class CustomerBotCheckOptions
{
    public const string SectionName = "CustomerBotCheck";

    public bool Enabled { get; set; } = true;

    public string SiteKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
