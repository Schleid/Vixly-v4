using System.Globalization;
using System.Text;

namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsItemWords.cs
//
// PURPOSE:
// Plain words for matching an item to the catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Lower case, no accents, letters and digits only, single spaces
//
// IMPORTANT NOTES:
// - Used on both sides of a match, so "Zapatos" and "zapatos" meet
// =========================================================
public static class OperationsItemWords
{
    public static string Plain(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder plain = new();
        bool space = true;

        foreach (char character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                plain.Append(char.ToLowerInvariant(character));
                space = false;
            }
            else if (!space)
            {
                plain.Append(' ');
                space = true;
            }
        }

        return plain.ToString().Trim();
    }
}
