using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsTokenHash.cs
//
// PURPOSE:
// Checks that a stored token hash is a SHA-256 hex string.
//
// RESPONSIBILITIES:
// - Refuse anything that is not 64 lowercase hex characters
//
// IMPORTANT NOTES:
// - Internal: only identity entities use it
// - The raw token never reaches the domain
// =========================================================
internal static class OperationsTokenHash
{
    private static readonly Regex Pattern = new("^[0-9a-f]{64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Require(string? hash)
    {
        if (hash is null || !Pattern.IsMatch(hash))
        {
            throw new DomainException(OperationsIdentityErrors.TokenHashInvalid);
        }

        return hash;
    }
}
