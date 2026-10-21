namespace Vixly.Customer.Web.Hosting;

// =========================================================
// FILE: CustomerHostingOptions.cs
//
// PURPOSE:
// Which host names are tenant addresses (section TenantHosting, H-1), as Operations.Web reads them.
//
// RESPONSIBILITIES:
// - Hold the base domains, slug suffix and development slug
//
// IMPORTANT NOTES:
// - Same section and values as Operations.Web
// =========================================================
public sealed class CustomerHostingOptions
{
    public const string SectionName = "TenantHosting";

    public string[] BaseDomains { get; set; } = Array.Empty<string>();

    public string SlugSuffix { get; set; } = string.Empty;

    public string DevelopmentSlug { get; set; } = string.Empty;
}
