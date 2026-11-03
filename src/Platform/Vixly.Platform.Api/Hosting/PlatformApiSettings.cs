using Microsoft.Extensions.Configuration;
using Vixly.Shared.Hosting.Settings;

namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformApiSettings.cs
//
// PURPOSE:
// The settings Platform API checks before it starts (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - List every setting the site checks before it starts
//
// IMPORTANT NOTES:
// - Need: the site cannot run without it; Should: it runs, with a warning in the log and on /health/ready
// - Kept in step with docs/global/SETTINGS.md
// =========================================================
public static class PlatformApiSettings
{
    public const string Site = "Platform API";

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
        VixlySettingRule.Should("PlatformApi:AllowedOrigins", VixlySettingKind.List, "The public site's address, so browsers may call the API."),
        VixlySettingRule.Need("PlatformBotCheck:SecretKey", VixlySettingKind.Secret, "Cloudflare Turnstile secret for the sign-up form (environment variable).", configuration => NotFalse(configuration, "PlatformBotCheck:Enabled")),
        VixlySettingRule.Need("PlatformInternal:Hosts", VixlySettingKind.List, "The host name Operations calls for companies, licences and the event feed; without it the internal API is off."),
        VixlySettingRule.Need("PlatformInternal:Callers", VixlySettingKind.Section, "The public keys of Operations, the public site and the edge, once PlatformInternal:Hosts is set.", configuration => Set(configuration, "PlatformInternal:Hosts:0")),
        VixlySettingRule.Should("PlatformPayments:Stripe:WebhookSecret", VixlySettingKind.Secret, "Stripe webhook signing secret (environment variable); card payment events are refused without it.",
            configuration => !string.Equals(configuration["PlatformPayments:Provider"], "simulated", StringComparison.OrdinalIgnoreCase)),
    };

    private static bool Set(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private static bool NotFalse(IConfiguration configuration, string key) => !string.Equals(configuration[key]?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
}
