using Microsoft.Extensions.Localization;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Components.Inspection;

// =========================================================
// FILE: OperationsInspectionWords.cs
//
// PURPOSE:
// Puts why a parcel was chosen, and a rule, into words in the reader's language (Slice 3C).
//
// RESPONSIBILITIES:
// - Turn "listed:Lithium batteries" or "value:500" into a sentence
// - Describe a rule for the setup page
//
// IMPORTANT NOTES:
// - The record keeps kind and value; the words come from OperationsText (L-10)
// =========================================================
public static class OperationsInspectionWords
{
    public static string Why(IStringLocalizer<OperationsText> text, string why, string? reason)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (why == "flagged")
        {
            return VixlyText.Format(text, "ui.inspection.why.flagged", "reason", reason ?? string.Empty);
        }

        if (why == "random" || reason is null || reason.IndexOf(':', StringComparison.Ordinal) is not (> 0 and var colon))
        {
            return text["ui.inspection.why.random"].Value;
        }

        string kind = reason[..colon];
        string value = reason[(colon + 1)..];
        return kind is "listed" or "value" or "carrier" or "country"
            ? VixlyText.Format(text, "ui.inspection.why." + kind, "value", value)
            : text["ui.inspection.why.rule"].Value;
    }

    public static string Rule(IStringLocalizer<OperationsText> text, OperationsInspectionRule rule, string currency)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(rule);

        return rule.Kind switch
        {
            OperationsInspectionRuleKind.ListedItems => text["ui.inspection.rule.listed"].Value,
            OperationsInspectionRuleKind.ValueAtLeast => VixlyText.Format(text, "ui.inspection.rule.value", new Dictionary<string, object> { ["value"] = rule.Value ?? string.Empty, ["currency"] = currency }),
            OperationsInspectionRuleKind.Carrier => VixlyText.Format(text, "ui.inspection.rule.carrier", "value", rule.Value ?? string.Empty),
            _ => VixlyText.Format(text, "ui.inspection.rule.country", "value", rule.Value ?? string.Empty)
        };
    }
}
