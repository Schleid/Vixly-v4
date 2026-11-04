using System.Globalization;
using System.Text;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsDeclarationText.cs
//
// PURPOSE:
// Checks the free text of inspections and reads declarations word by word (Slice 3C).
//
// RESPONSIBILITIES:
// - Trim and check names, notes and instructions
// - Turn a declaration or a list of words into comparable words: lower case, without accents
//
// IMPORTANT NOTES:
// - Notes may span lines; nothing that looks like markup or a link
// - Accents are ignored so "médicament" matches "medicament" (fr/es declarations)
// =========================================================
public static class OperationsDeclarationText
{
    public static string Required(string? value, int maxLength, Error error) =>
        Optional(value, maxLength) ?? throw new DomainException(error);

    public static string? Optional(string? value, int maxLength)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        if (text is null)
        {
            return null;
        }

        return text.Length > maxLength || text.IndexOfAny(['<', '>']) >= 0 || text.Any(character => char.IsControl(character) && character != '\n')
            || text.Contains("://", StringComparison.Ordinal)
            ? throw new DomainException(OperationsInspectionErrors.TextInvalid)
            : text;
    }

    // "Lithium battery, power-bank" → ["lithium", "battery", "power", "bank"]
    public static IReadOnlyList<string> Words(string? text)
    {
        List<string> words = new();
        StringBuilder current = new();

        foreach (char character in Fold(text ?? string.Empty))
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }

    // Each entry of a comma-separated list, as its own sequence of words: "power bank" is matched as two words in a row.
    public static IReadOnlyList<IReadOnlyList<string>> Phrases(string? list) =>
        (list ?? string.Empty).Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Words).Where(words => words.Count > 0).ToList();

    // True when the phrase appears in the declaration as whole words, in order.
    public static bool Contains(IReadOnlyList<string> declaration, IReadOnlyList<string> phrase)
    {
        for (int start = 0; start + phrase.Count <= declaration.Count; start++)
        {
            int index = 0;

            while (index < phrase.Count && Same(declaration[start + index], phrase[index]))
            {
                index++;
            }

            if (index == phrase.Count)
            {
                return true;
            }
        }

        return false;
    }

    // A plural "s" on either side still matches: battery/batteries is listed as both, but "aerosol" also finds "aerosols".
    private static bool Same(string declared, string listed) =>
        declared == listed || declared == listed + "s" || listed == declared + "s";

    private static string Fold(string text)
    {
        StringBuilder folded = new(text.Length);

        foreach (char character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                folded.Append(char.ToLowerInvariant(character));
            }
        }

        return folded.ToString();
    }
}
