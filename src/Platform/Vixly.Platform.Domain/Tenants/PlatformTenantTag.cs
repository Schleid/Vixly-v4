using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantTag.cs
//
// PURPOSE:
// An internal label on a tenant, used to filter the directory
// (D-T16).
//
// RESPONSIBILITIES:
// - Validate and hold one tag
//
// IMPORTANT NOTES:
// - Owned by PlatformTenant; changed only through it
// - Typed by staff and not translated, like custom role names
// - Never shown to the tenant or sent to another plane
// =========================================================
public sealed class PlatformTenantTag : AuditableEntity
{
    private static readonly Regex Pattern = new(
        "^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private PlatformTenantTag()
    {
        Value = string.Empty;
    }

    private PlatformTenantTag(Guid platformTenantId, string value, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        Value = value;
    }

    public Guid PlatformTenantId { get; private set; }

    public string Value { get; private set; }

    public static string Normalize(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Length is < PlatformTenantLimits.TagMinLength or > PlatformTenantLimits.TagMaxLength
            || !Pattern.IsMatch(normalized))
        {
            throw new DomainException(PlatformTenantErrors.TagInvalid);
        }

        return normalized;
    }

    internal static PlatformTenantTag Create(Guid platformTenantId, string normalizedValue, DateTimeOffset createdUtc)
    {
        return new PlatformTenantTag(platformTenantId, normalizedValue, createdUtc);
    }
}
