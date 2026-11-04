using System.Security.Cryptography;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsPrintPairingCode.cs
//
// PURPOSE:
// The one-time code a phone scans to join a warehouse as Vixly Print (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Make a new code; tidy a typed or scanned one
//
// IMPORTANT NOTES:
// - 12 characters without look-alikes (no 0, O, 1, I), shown as XXXX-XXXX-XXXX so it can also be typed
// =========================================================
public static class OperationsPrintPairingCode
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string New()
    {
        char[] characters = new char[12];

        for (int index = 0; index < characters.Length; index++)
        {
            characters[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        string code = new(characters);
        return code[..4] + "-" + code[4..8] + "-" + code[8..];
    }

    public static string? Tidy(string? typed)
    {
        string text = new string((typed ?? string.Empty).Where(character => character != '-' && !char.IsWhiteSpace(character)).ToArray()).ToUpperInvariant();
        return text.Length == 12 && text.All(Alphabet.Contains) ? text[..4] + "-" + text[4..8] + "-" + text[8..] : null;
    }
}
