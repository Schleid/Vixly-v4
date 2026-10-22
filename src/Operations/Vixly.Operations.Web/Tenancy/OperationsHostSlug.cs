namespace Vixly.Operations.Web.Tenancy;

// =========================================================
// FILE: OperationsHostSlug.cs
//
// PURPOSE:
// Reads the tenant slug from a host name (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Turn {slug}.<base domain> into {slug}
//
// IMPORTANT NOTES:
// - Only one label before the base domain: a.b.<base> is no tenant
// - localhost maps to the configured development slug
// =========================================================
public static class OperationsHostSlug
{
    public static string? From(string host, Hosting.OperationsHostingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        string name = host.Trim().TrimEnd('.').ToLowerInvariant();

        if (name == "localhost")
        {
            return string.IsNullOrWhiteSpace(options.DevelopmentSlug) ? null : options.DevelopmentSlug.Trim().ToLowerInvariant();
        }

        foreach (string domain in options.BaseDomains.Where(domain => !string.IsNullOrWhiteSpace(domain)))
        {
            string suffix = "." + domain.Trim().TrimStart('.').ToLowerInvariant();

            if (!name.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            string label = name[..^suffix.Length];

            if (label.Length == 0 || label.Contains('.', StringComparison.Ordinal))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(options.SlugSuffix) && label.EndsWith(options.SlugSuffix, StringComparison.Ordinal))
            {
                label = label[..^options.SlugSuffix.Length];
            }

            return label.Length == 0 ? null : label;
        }

        return null;
    }
}
