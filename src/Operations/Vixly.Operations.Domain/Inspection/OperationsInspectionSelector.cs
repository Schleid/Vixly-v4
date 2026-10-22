namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionSelector.cs
//
// PURPOSE:
// Decides whether a company's rules send a parcel to compliance inspection, and why (Slice 3C design section 4).
//
// RESPONSIBILITIES:
// - Find the first rule that matches: a listed item named in the declaration, a value, a carrier, a destination
// - List the restricted items that apply to a destination and carrier, for the inspector
//
// IMPORTANT NOTES:
// - The reason is stored as kind and value ("listed:Lithium batteries", "value:500"); pages put it in words in the reader's language
// - A listed item counts only where it applies (its destinations and carriers)
// =========================================================
public static class OperationsInspectionSelector
{
    public static string? FirstMatch(OperationsInspectionSubject subject, IReadOnlyList<OperationsInspectionRule> rules, IReadOnlyList<OperationsRestrictedItem> items)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(items);

        foreach (OperationsInspectionRule rule in rules.Where(rule => rule.IsActive).OrderBy(rule => rule.Kind).ThenBy(rule => rule.CreatedUtc))
        {
            if (rule.Kind == OperationsInspectionRuleKind.ListedItems)
            {
                if (Named(subject, items).FirstOrDefault() is { } item)
                {
                    return "listed:" + item.Name;
                }
            }
            else if (rule.Matches(subject))
            {
                return rule.Kind switch
                {
                    OperationsInspectionRuleKind.ValueAtLeast => "value:" + rule.Value,
                    OperationsInspectionRuleKind.Carrier => "carrier:" + rule.Value,
                    _ => "country:" + rule.Value
                };
            }
        }

        return null;
    }

    // The listed items a declaration names, where they apply.
    public static IReadOnlyList<OperationsRestrictedItem> Named(OperationsInspectionSubject subject, IReadOnlyList<OperationsRestrictedItem> items)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(items);

        return items.Where(item => item.AppliesTo(subject.CountryCode, subject.Carrier) && item.IsNamedIn(subject.Declaration)).ToList();
    }

    // Everything the inspector should look for: the items that apply to this destination and carrier.
    public static IReadOnlyList<OperationsRestrictedItem> Applicable(OperationsInspectionSubject subject, IReadOnlyList<OperationsRestrictedItem> items)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(items);

        return items.Where(item => item.AppliesTo(subject.CountryCode, subject.Carrier)).OrderBy(item => item.Kind).ThenBy(item => item.Name, StringComparer.CurrentCulture).ToList();
    }
}
