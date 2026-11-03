using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Resources;

namespace Vixly.Operations.Infrastructure.Identity;

// =========================================================
// FILE: OperationsRoleNames.cs
//
// PURPOSE:
// Names a standard role in the company's language, from OperationsText (owner, 2026-10-29).
//
// RESPONSIBILITIES:
// - Read ui.roles.standard.{key} in en, fr or es
//
// IMPORTANT NOTES:
// - Switches the UI culture only for the read, then puts it back
// - An unknown key gives the key itself, so a role is never created without a name
// =========================================================
internal sealed class OperationsRoleNames : IOperationsRoleNames
{
    private readonly IStringLocalizer<OperationsText> _text;

    public OperationsRoleNames(IStringLocalizer<OperationsText> text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public string Name(string key, string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(language is "fr" or "es" ? language : "en");
            LocalizedString name = _text["ui.roles.standard." + key];
            return name.ResourceNotFound ? key : name.Value;
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
