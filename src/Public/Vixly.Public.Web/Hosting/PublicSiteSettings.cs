using Microsoft.Extensions.Configuration;
using Vixly.Shared.Hosting.Settings;

namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicSiteSettings.cs
//
// PURPOSE:
// The settings the public site checks before it starts (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - List every setting the site checks before it starts
//
// IMPORTANT NOTES:
// - Need: the site cannot run without it; Should: it runs, with a warning in the log and on /health/ready
// - Kept in step with docs/global/SETTINGS.md
// =========================================================
public static class PublicSiteSettings
{
    public const string Site = "Public site";

    public static IReadOnlyList<VixlySettingRule> All { get; } = new[]
    {
        VixlySettingRule.Need("PublicSite:PlatformApiBaseUrl", VixlySettingKind.Url, "Platform API's address."),
        VixlySettingRule.Need("PublicSite:CanonicalBaseUrl", VixlySettingKind.Url, "The public site's own address, for links and search engines."),
        VixlySettingRule.Should("PublicSite:SalesEmail", VixlySettingKind.Email, "The address shown for sales questions."),
        VixlySettingRule.Should("PublicSite:CallerKeyId", VixlySettingKind.Text, "The key id Platform knows the public site by; without it Platform counts limits per server, not per visitor."),
        VixlySettingRule.Should("PublicSite:CallerPrivateKey", VixlySettingKind.EcPrivateKey, "The private key the public site signs with (environment variable).", configuration => Set(configuration, "PublicSite:CallerKeyId")),
        VixlySettingRule.Need("PublicSite:TurnstileSiteKey", VixlySettingKind.Text, "Cloudflare Turnstile site key for sign-up and contact forms.", configuration => string.Equals(configuration["PublicSite:RegistrationOpen"], "true", StringComparison.OrdinalIgnoreCase)),
        VixlySettingRule.Should("PublicSite:KeysPath", VixlySettingKind.Folder, "Where form keys are kept, outside the site folder; without it open forms fail after each restart."),
    };

    private static bool Set(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private static bool NotFalse(IConfiguration configuration, string key) => !string.Equals(configuration[key]?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
}
