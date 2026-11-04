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

        // Slice 6C: a cause in the reader's language (the company's own words kept on the event), the customer's line, and where a copy comes from.
        if (line.Arguments.TryGetValue("cause", out string? cause))
        {
            values["cause"] = line.Arguments.ContainsKey("label_en")
                ? Label(line.Arguments.GetValueOrDefault("label_en"), line.Arguments.GetValueOrDefault("label_fr"), line.Arguments.GetValueOrDefault("label_es"))
                : text["ui.event_reason." + cause].Value;
            values["message"] = line.Arguments.TryGetValue("message", out string? message) ? " — " + message : string.Empty;
        }
        else
        {
            // Slice 7: customs held, with the line staff added or none.
            values["message"] = line.Arguments.TryGetValue("message", out string? message) ? " — " + message : string.Empty;
        }

        if (line.Arguments.TryGetValue("source", out string? source))
        {
            return VixlyText.Format(text, Key(line.Code), values) + " · " + VixlyText.Format(text, "ui.events.via", "source", source);
        }

        return VixlyText.Format(text, Key(line.Code), values);
    }

    // A company's own cause in the current language, English when it has no words in it.
    public static string Label(string? english, string? french, string? spanish) => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "fr" => french ?? english ?? string.Empty,
        "es" => spanish ?? english ?? string.Empty,
        _ => english ?? string.Empty
    };

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
            "mode" => "ui.dispatch.mode." + value,
            "failure" => "ui.delivery.failure." + value,
            _ => null
        };

        if (argument == "country" && value.Length == 2)
        {
            try
            {
                return new System.Globalization.RegionInfo(value).DisplayName;
            }
            catch (ArgumentException)
            {
                return value;
            }
        }

        return key is not null && text[key] is { ResourceNotFound: false } found ? found.Value : value;
    }
}
