using Microsoft.Extensions.Configuration;
using Vixly.Shared.Hosting.Settings;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformWebSettings.cs
//
// PURPOSE:
// The settings Control checks before it starts (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - List every setting the site checks before it starts
//
// IMPORTANT NOTES:
// - Need: the site cannot run without it; Should: it runs, with a warning in the log and on /health/ready
// - Kept in step with docs/global/SETTINGS.md
// =========================================================
public static class PlatformWebSettings
{
    public const string Site = "Control";

    public static IReadOnlyList<VixlySettingRule> All { get; } = new[]
    {
        VixlySettingRule.Need("ConnectionStrings:Platform", VixlySettingKind.Text, "The Platform database."),
        VixlySettingRule.Need("PlatformHosting:ConsoleBaseUrl", VixlySettingKind.Url, "Control's address, used in emails."),
        VixlySettingRule.Need("PlatformHosting:PublicBaseUrl", VixlySettingKind.Url, "The public site's address, used in emails."),
        VixlySettingRule.Need("PlatformTenants:TenantBaseDomain", VixlySettingKind.Text, "The domain under which each company gets its address."),
        VixlySettingRule.Need("PlatformMail:FromAddress", VixlySettingKind.Email, "The address Control emails are sent from."),
        VixlySettingRule.Need("PlatformMail:SmtpHost", VixlySettingKind.Text, "The mail server.", configuration => !Set(configuration, "PlatformMail:PickupDirectory")),
        VixlySettingRule.Should("PlatformMail:Password", VixlySettingKind.Text, "The mail password (environment variable); without it every email fails.", configuration => Set(configuration, "PlatformMail:UserName")),
        VixlySettingRule.Should("PlatformPayments:Stripe:SecretKey", VixlySettingKind.Secret, "Stripe secret key (environment variable); card payments are off without it.",
            configuration => !string.Equals(configuration["PlatformPayments:Provider"], "simulated", StringComparison.OrdinalIgnoreCase)),
        VixlySettingRule.Should("PlatformSales:TechOs:Url", VixlySettingKind.Url, "TechOS's address for Vixly sale events (environment variable); the sales feed is off without it, events still wait."),
        VixlySettingRule.Should("PlatformSales:TechOs:Secret", VixlySettingKind.Secret, "The secret shared with TechOS for the sales feed (environment variable); the sales feed is off without it.",
            configuration => Set(configuration, "PlatformSales:TechOs:Url"), minLength: 16),
    };

    private static bool Set(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private static bool NotFalse(IConfiguration configuration, string key) => !string.Equals(configuration[key]?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
}
