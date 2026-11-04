using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformPortalSlugFormat.cs
//
// PURPOSE:
// The format and the reserved floor of portal slugs, the first
// label of a tenant's address ({slug}.<base domain>).
//
// RESPONSIBILITIES:
// - Normalise and validate a slug as a DNS label
// - Hold the reserved names that configuration can extend but
//   never shrink (Global Standard H-2)
//
// IMPORTANT NOTES:
// - Lowercase letters, digits and inner single hyphens, 3 to 63
//   characters
// - Anything ending in -staging is reserved, so a tenant can
//   never look like a staging host
// =========================================================
public static class PlatformPortalSlugFormat
{
    private static readonly Regex Pattern = new(
        "^[a-z0-9](?:[a-z0-9]|-(?!-))*[a-z0-9]$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<string> ReservedFloor { get; } = new[]
    {
        "www", "mail", "vixly", "vixly-control", "vixly-api", "control", "api", "admin", "edge", "status", "staging",

        // The Operations internal host shares the tenant wildcard: vixly-ops-internal in production, ops-internal on staging.
        "vixly-ops-internal", "ops-internal"
    };

    public static string Normalize(string? value)
    {
        if (!TryNormalize(value, out string? slug))
        {
            throw new DomainException(PlatformTenantErrors.SlugInvalid);
        }

        return slug;
    }

    public static bool TryNormalize(
        string? value,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? slug)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        bool valid = normalized.Length is >= PlatformTenantLimits.SlugMinLength and <= PlatformTenantLimits.SlugMaxLength
            && Pattern.IsMatch(normalized);

        slug = valid ? normalized : null;
        return valid;
    }

    public static bool IsReserved(string slug, IEnumerable<string> configured)
    {
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(configured);

        return slug.EndsWith("-staging", StringComparison.Ordinal)
            || ReservedFloor.Contains(slug, StringComparer.Ordinal)
            || configured.Any(name => string.Equals(name?.Trim(), slug, StringComparison.OrdinalIgnoreCase));
    }
}
