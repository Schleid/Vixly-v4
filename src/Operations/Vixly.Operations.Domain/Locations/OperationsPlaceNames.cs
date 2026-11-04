using System.Globalization;
using System.Text;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsPlaceNames.cs
//
// PURPOSE:
// Compares place names the way people type them: Jérémie, JEREMIE and jeremie are one commune (Slice 4B).
//
// RESPONSIBILITIES:
// - Fold accents, case, hyphens and spaces
//
// IMPORTANT NOTES:
// - Used for home delivery areas
// =========================================================
public static class OperationsPlaceNames
{
    public static string Fold(string? text)
    {
        string decomposed = (text ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        StringBuilder folded = new(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                folded.Append(char.IsWhiteSpace(character) || character == '-' ? ' ' : char.ToLowerInvariant(character));
            }
        }

        return string.Join(' ', folded.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
