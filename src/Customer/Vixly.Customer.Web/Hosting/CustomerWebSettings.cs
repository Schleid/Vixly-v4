using Microsoft.Extensions.Configuration;
using Vixly.Shared.Hosting.Settings;

namespace Vixly.Customer.Web.Hosting;

// =========================================================
// FILE: CustomerWebSettings.cs
//
// PURPOSE:
// The settings the customer portal checks before it starts (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - List every setting the site checks before it starts
//
// IMPORTANT NOTES:
// - Need: the site cannot run without it; Should: it runs, with a warning in the log and on /health/ready
// - Kept in step with docs/global/SETTINGS.md
// =========================================================
public static class CustomerWebSettings
{
    public const string Site = "Customer portal";

    public static IReadOnlyList<VixlySettingRule> All { get; } = new[]
    {
        VixlySettingRule.Need("ConnectionStrings:Customer", VixlySettingKind.Text, "The customer portal database (its keys)."),
        VixlySettingRule.Need("TenantHosting:BaseDomains", VixlySettingKind.List, "The domains under which company addresses are found."),
        VixlySettingRule.Need("CustomerOperations:BaseUrl", VixlySettingKind.Url, "Operations API's address."),
        VixlySettingRule.Need("CustomerOperations:CallerKeyId", VixlySettingKind.Text, "The key id Operations knows the portal by."),
        VixlySettingRule.Need("CustomerOperations:CallerPrivateKey", VixlySettingKind.EcPrivateKey, "The private key the portal signs its calls with (environment variable)."),
        VixlySettingRule.Need("CustomerBotCheck:SiteKey", VixlySettingKind.Text, "Cloudflare Turnstile site key for the sign-up form.", configuration => NotFalse(configuration, "CustomerBotCheck:Enabled")),
        VixlySettingRule.Need("CustomerBotCheck:SecretKey", VixlySettingKind.Secret, "Cloudflare Turnstile secret (environment variable).", configuration => NotFalse(configuration, "CustomerBotCheck:Enabled")),
    };

    private static bool Set(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private static bool NotFalse(IConfiguration configuration, string key) => !string.Equals(configuration[key]?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
}
