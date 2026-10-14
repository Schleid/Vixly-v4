using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformTokenHash.cs
//
// PURPOSE:
// Validates the stored form of secrets the identity model
// keeps only as hashes: session tokens, account tokens and
// recovery codes.
//
// RESPONSIBILITIES:
// - Accept only a lowercase hex SHA-256 digest
//
// IMPORTANT NOTES:
// - The raw secret never reaches the domain or the database
// =========================================================
internal static class PlatformTokenHash
{
    private static readonly Regex Pattern = new(
        "^[0-9a-f]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Require(string? hash)
    {
        if (hash is null || !Pattern.IsMatch(hash))
        {
            throw new DomainException(PlatformIdentityErrors.TokenHashInvalid);
        }

        return hash;
    }
}
