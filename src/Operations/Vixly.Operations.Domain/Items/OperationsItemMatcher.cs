namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsItemMatcher.cs
//
// PURPOSE:
// Finds the catalogue entry that fits the customer's words (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Compare the catalogue's description and its "also matches" phrases with the words, as whole words
// - The longest phrase found wins, so "phone case" beats "phone"
//
// IMPORTANT NOTES:
// - No match: null, and staff classify the item
// =========================================================
public static class OperationsItemMatcher
{
    public static OperationsItemCatalogueEntry? Find(IEnumerable<OperationsItemCatalogueEntry> catalogue, string? words)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        string text = " " + OperationsItemWords.Plain(words) + " ";

        if (text.Length <= 2)
        {
            return null;
        }

        OperationsItemCatalogueEntry? best = null;
        int bestLength = 0;

        foreach (OperationsItemCatalogueEntry entry in catalogue)
        {
            foreach (string phrase in entry.Phrases())
            {
                if (phrase.Length > bestLength && text.Contains(" " + phrase + " ", StringComparison.Ordinal))
                {
                    (best, bestLength) = (entry, phrase.Length);
                }
            }
        }

        return best;
    }
}
