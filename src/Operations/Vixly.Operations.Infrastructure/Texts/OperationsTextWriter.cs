using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Texts;
using Vixly.Operations.Resources;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Texts;

// =========================================================
// FILE: OperationsTextWriter.cs
//
// PURPOSE:
// Words a notice as a short text in the customer's language (Slice 9).
//
// RESPONSIBILITIES:
// - The sentence "text.notice.{notice}" from OperationsText with its values (shipment, place, code)
//
// IMPORTANT NOTES:
// - Short on purpose: an SMS segment is 160 characters (70 with accents); the company and the link are added by the caller
// =========================================================
public sealed class OperationsTextWriter : IOperationsTextWriter
{
    private readonly IStringLocalizer<OperationsText> _text;

    public OperationsTextWriter(IStringLocalizer<OperationsText> text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public string Sentence(OperationsTextNotices notice, LanguageCode language, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(values);
        Dictionary<string, object> all = new(StringComparer.Ordinal) { ["shipment"] = string.Empty, ["place"] = string.Empty, ["code"] = string.Empty };

        foreach ((string name, string value) in values)
        {
            all[name] = value;
        }

        CultureInfo previous = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language.Value);
            return VixlyText.Format(_text, "text.notice." + notice, all).Replace("  ", " ", StringComparison.Ordinal).Trim();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
