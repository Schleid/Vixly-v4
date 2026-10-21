using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Components.Parcels;

// =========================================================
// FILE: OperationsTrackingText.cs
//
// PURPOSE:
// Words one tracking line for staff (Slice 3B).
//
// RESPONSIBILITIES:
// - Fill the code's sentence with its arguments, each choice in the reader's language
//
// IMPORTANT NOTES:
// - Arguments from fixed lists use their own words; codes and numbers are shown as they are
// =========================================================
public static class OperationsTrackingText
{
    private const string Prefix = "ops.tracking.";

    public static string Describe(IStringLocalizer<OperationsText> text, OperationsTrackingLine line)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(line);

        Dictionary<string, object> values = line.Arguments.ToDictionary(pair => pair.Key, pair => (object)Choice(text, pair.Key, pair.Value), StringComparer.Ordinal);
        return VixlyText.Format(text, Key(line.Code), values);
    }

    // ops.tracking.put_away is worded by ui.tracking.put_away.
    public static string Key(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return "ui.tracking." + (code.StartsWith(Prefix, StringComparison.Ordinal) ? code[Prefix.Length..] : code);
    }

    private static string Choice(IStringLocalizer<OperationsText> text, string argument, string value)
    {
        string? key = argument switch
        {
            "condition" => "ui.receiving.condition." + value,
            "kind" => "ui.receiving.photo." + value,
            "reason" => "ui.hold." + value,
            "verified_by" => "ui.parcels.verified." + value,
            "result" => "ui.inspection.result." + value,
            "why" => "ui.inspection.reason." + value,
            _ => null
        };

        return key is not null && text[key] is { ResourceNotFound: false } found ? found.Value : value;
    }
}
