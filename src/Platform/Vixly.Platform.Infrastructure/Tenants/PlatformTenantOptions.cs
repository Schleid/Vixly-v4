namespace Vixly.Platform.Infrastructure.Tenants;

// =========================================================
// FILE: PlatformTenantOptions.cs
//
// PURPOSE:
// Configuration of tenant management (section PlatformTenants).
//
// RESPONSIBILITIES:
// - Hold the extra reserved slugs, the legal versions and the
//   tenant base domain
//
// IMPORTANT NOTES:
// - No secrets here
// - The H-2 reserved floor is in code and cannot be removed
// =========================================================
public sealed class PlatformTenantOptions
{
    public const string SectionName = "PlatformTenants";

    public string[] ReservedSlugs { get; set; } = Array.Empty<string>();

    public string TermsVersion { get; set; } = string.Empty;

    public string PrivacyVersion { get; set; } = string.Empty;

    public string TenantBaseDomain { get; set; } = "techcrafters.ltd";
}
