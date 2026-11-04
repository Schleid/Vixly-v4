namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsHostingOptions.cs
//
// PURPOSE:
// Which host names are tenant addresses (section TenantHosting, H-1).
//
// RESPONSIBILITIES:
// - Hold the base domains, the slug suffix and the development slug
//
// IMPORTANT NOTES:
// - No host name literal in code (H-1)
// - Staging hosts {slug}-staging.<base> strip the suffix
// =========================================================
public sealed class OperationsHostingOptions
{
    public const string SectionName = "TenantHosting";

    public string[] BaseDomains { get; set; } = Array.Empty<string>();

    public string SlugSuffix { get; set; } = string.Empty;

    public string DevelopmentSlug { get; set; } = string.Empty;
}
