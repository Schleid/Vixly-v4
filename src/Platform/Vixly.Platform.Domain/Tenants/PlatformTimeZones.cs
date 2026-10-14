using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTimeZones.cs
//
// PURPOSE:
// Validation of the IANA time zone a tenant works in (D-T15).
//
// RESPONSIBILITIES:
// - Accept IANA ids only (for example America/Toronto) and UTC
//
// IMPORTANT NOTES:
// - Windows ids such as "Eastern Standard Time" are refused, so
//   stored values mean the same on every host
// - Uses the BCL conversion table, so no package is needed
// =========================================================
public static class PlatformTimeZones
{
    public const string Utc = "UTC";

    public static string Normalize(string? value)
    {
        string trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            return Utc;
        }

        if (string.Equals(trimmed, Utc, StringComparison.OrdinalIgnoreCase))
        {
            return Utc;
        }

        if (trimmed.Length > PlatformTenantLimits.TimeZoneMaxLength
            || !trimmed.Contains('/', StringComparison.Ordinal)
            || !TimeZoneInfo.TryConvertIanaIdToWindowsId(trimmed, out _))
        {
            throw new DomainException(PlatformTenantErrors.TimeZoneInvalid);
        }

        return trimmed;
    }
}
