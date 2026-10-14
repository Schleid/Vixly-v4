using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantCodeFormat.cs
//
// PURPOSE:
// The format of a tenant's internal code, read by other planes.
//
// RESPONSIBILITIES:
// - Validate a code (V3 codes included)
// - Build a new code from random bytes (D-T13)
//
// IMPORTANT NOTES:
// - New codes are TEN- plus 6 Crockford base-32 characters
//   (no I, L, O or U), about 1 billion values
// - V3 codes (TEN- plus 4 hex) stay valid and never change
// - The randomness comes from the caller, so the domain stays
//   testable and free of crypto services
// =========================================================
public static class PlatformTenantCodeFormat
{
    public const int RandomByteCount = 4;

    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private static readonly Regex Pattern = new(
        "^[A-Z0-9](?:[A-Z0-9-]*[A-Z0-9])?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Validate(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized.Length is < PlatformTenantLimits.TenantCodeMinLength or > PlatformTenantLimits.TenantCodeMaxLength
            || !Pattern.IsMatch(normalized))
        {
            throw new DomainException(PlatformTenantErrors.CodeInvalid);
        }

        return normalized;
    }

    public static string FromRandom(ReadOnlySpan<byte> random)
    {
        if (random.Length < RandomByteCount)
        {
            throw new DomainException(PlatformTenantErrors.CodeInvalid);
        }

        ulong bits = 0;

        for (int index = 0; index < RandomByteCount; index++)
        {
            bits = (bits << 8) | random[index];
        }

        Span<char> characters = stackalloc char[6];

        for (int index = characters.Length - 1; index >= 0; index--)
        {
            characters[index] = Alphabet[(int)(bits & 31)];
            bits >>= 5;
        }

        return "TEN-" + new string(characters);
    }
}
