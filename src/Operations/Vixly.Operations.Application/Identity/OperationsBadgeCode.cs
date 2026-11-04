using System.Security.Cryptography;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsBadgeCode.cs
//
// PURPOSE:
// The code printed on a staff badge (Slice 12A).
//
// RESPONSIBILITIES:
// - Make a new random code
// - Tidy a scanned code before it is hashed
//
// IMPORTANT NOTES:
// - Capital letters and digits only (no 0, O, 1, I), so a scanner typing on any keyboard layout reads it the same; 125 bits of chance
// - The prefix tells a badge from a parcel or shelf code at a scan field
// =========================================================
public static class OperationsBadgeCode
{
    public const string Prefix = "SB";

    public const int Length = 27;

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string New()
    {
        char[] characters = new char[Length - Prefix.Length];

        for (int index = 0; index < characters.Length; index++)
        {
            characters[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return Prefix + new string(characters);
    }

    // Scanners may add spaces, a symbology prefix (]C0 to ]C9) or lower case.
    public static string? Tidy(string? scanned)
    {
        string text = new string((scanned ?? string.Empty).Where(character => !char.IsWhiteSpace(character)).ToArray()).ToUpperInvariant();

        if (text.Length > 3 && text[0] == ']')
        {
            text = text[3..];
        }

        return text.Length == Length && text.StartsWith(Prefix, StringComparison.Ordinal) && text.Skip(Prefix.Length).All(Alphabet.Contains) ? text : null;
    }
}
