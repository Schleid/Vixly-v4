using Microsoft.Extensions.Configuration;
using Vixly.Shared.Hosting.Settings;

namespace Vixly.Operations.Api.Hosting;

// =========================================================
// FILE: OperationsApiSettings.cs
//
// PURPOSE:
// The settings Operations API checks before it starts (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - List every setting the site checks before it starts
//
// IMPORTANT NOTES:
// - Need: the site cannot run without it; Should: it runs, with a warning in the log and on /health/ready
// - Kept in step with docs/global/SETTINGS.md
// =========================================================
public static class OperationsApiSettings
{
    public const string Site = "Operations API";

    public static IReadOnlyList<VixlySettingRule> All { get; } = new[]
    {
        VixlySettingRule.Need("ConnectionStrings:Operations", VixlySettingKind.Text, "The Operations database."),
        VixlySettingRule.Need(Vixly.Shared.Hosting.Security.VixlyKeyRingEncryption.KeySetting, VixlySettingKind.Secret, "Locks the key ring kept in the database (32 random bytes, base64; environment variable). The same value on Operations Web and Operations API.", minLength: Vixly.Shared.Hosting.Security.VixlyKeyRingEncryption.KeyTextLength),
        VixlySettingRule.Need("OperationsPlatform:BaseUrl", VixlySettingKind.Url, "Platform API's address, for companies, licences and the event feed."),
        VixlySettingRule.Need("OperationsPlatform:CallerKeyId", VixlySettingKind.Text, "The key id Platform knows Operations by."),
        VixlySettingRule.Need("OperationsPlatform:CallerPrivateKey", VixlySettingKind.EcPrivateKey, "The private key Operations signs its Platform calls with (environment variable)."),
        VixlySettingRule.Need("OperationsLinks:PortalUrlTemplate", VixlySettingKind.Template, "Each company's address in emails and texts, with {slug}."),
        VixlySettingRule.Need("OperationsFiles:Root", VixlySettingKind.Folder, "Uploaded photos, signatures and documents; shared by Operations Web and API, outside every site folder."),
        VixlySettingRule.Need("OperationsMail:FromAddress", VixlySettingKind.Email, "The address company emails are sent from."),
        VixlySettingRule.Need("OperationsMail:SmtpHost", VixlySettingKind.Text, "The mail server.", configuration => !Set(configuration, "OperationsMail:PickupDirectory")),
        VixlySettingRule.Should("OperationsMail:Password", VixlySettingKind.Text, "The mail password (environment variable); without it every email fails.", configuration => Set(configuration, "OperationsMail:UserName")),
        VixlySettingRule.Need("OperationsInternal:Hosts", VixlySettingKind.List, "The host name the customer portal calls."),
        VixlySettingRule.Need("OperationsInternal:Callers", VixlySettingKind.Section, "The public key of the customer portal."),
    };

    private static bool Set(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private static bool NotFalse(IConfiguration configuration, string key) => !string.Equals(configuration[key]?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
}
