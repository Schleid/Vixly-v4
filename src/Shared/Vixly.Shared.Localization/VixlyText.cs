using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyText.cs
//
// PURPOSE:
// Turns resource keys and Errors into display text with
// named placeholders (L-3).
//
// RESPONSIBILITIES:
// - Fill {name} placeholders from named values, formatted in
//   the current culture
// - Resolve an Error by its code, looking in the plane's
//   resources first and the shared resources second
//
// IMPORTANT NOTES:
// - Never concatenates sentences; never uses positional
//   placeholders
// - A missing key shows the key itself (and the build check
//   prevents that from shipping)
// =========================================================
public static class VixlyText
{
    private static readonly Regex Placeholder = new(
        @"\{(?<name>[a-z][A-Za-z0-9]*)\}",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Format(
        IStringLocalizer localizer,
        string key,
        IReadOnlyDictionary<string, object>? values = null)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        LocalizedString text = localizer[key];

        return Fill(text.Value, values);
    }

    public static string Format(IStringLocalizer localizer, string key, string name, object value)
    {
        return Format(localizer, key, new Dictionary<string, object>(StringComparer.Ordinal) { [name] = value });
    }

    public static string Describe(IStringLocalizer planeText, IStringLocalizer sharedText, Error error)
    {
        ArgumentNullException.ThrowIfNull(planeText);
        ArgumentNullException.ThrowIfNull(sharedText);
        ArgumentNullException.ThrowIfNull(error);

        LocalizedString text = planeText[error.Code];

        if (text.ResourceNotFound)
        {
            text = sharedText[error.Code];
        }

        return Fill(text.Value, error.Arguments);
    }

    public static string Fill(string template, IReadOnlyDictionary<string, object>? values)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (values is null || values.Count == 0)
        {
            return template;
        }

        return Placeholder.Replace(template, match =>
            values.TryGetValue(match.Groups["name"].Value, out object? value)
                ? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
                : match.Value);
    }
}
