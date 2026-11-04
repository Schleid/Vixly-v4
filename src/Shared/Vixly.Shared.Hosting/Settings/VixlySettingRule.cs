using Microsoft.Extensions.Configuration;

namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingRule.cs
//
// PURPOSE:
// One setting a site needs, and how to check it (settings check, 2026-10-23).
//
// RESPONSIBILITIES:
// - Name the setting, its shape, whether the site can run without it, and when it applies
// - Say in a few words what it is for
//
// IMPORTANT NOTES:
// - Key uses the configuration form (Section:Key); the environment variable form (Section__Key) is shown to the operator
// - Required: the site cannot run without it. Otherwise it is a warning
// - A rule with a condition applies only when the condition holds
// =========================================================
public sealed record VixlySettingRule(
    string Key,
    VixlySettingKind Kind,
    bool Required,
    string Purpose,
    Func<IConfiguration, bool>? When = null,
    int MinLength = 0)
{
    public static VixlySettingRule Need(string key, VixlySettingKind kind, string purpose, Func<IConfiguration, bool>? when = null, int minLength = 0) =>
        new(key, kind, true, purpose, when, minLength);

    public static VixlySettingRule Should(string key, VixlySettingKind kind, string purpose, Func<IConfiguration, bool>? when = null, int minLength = 0) =>
        new(key, kind, false, purpose, when, minLength);

    public string EnvironmentName => Key.Replace(":", "__", StringComparison.Ordinal);
}
