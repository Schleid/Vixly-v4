using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSitePage.cs
//
// PURPOSE:
// What every tenant site page needs: the site, its language, its absolute address and the tenant texts with their defaults.
//
// RESPONSIBILITIES:
// - Give a tenant text, or the default text with the company name
// - Build absolute and language addresses
//
// IMPORTANT NOTES:
// - Built by the site endpoints; never holds text of its own
// =========================================================
public sealed class OperationsSitePage
{
    public OperationsSitePage(OperationsPublicSite site, string language, string origin, string path)
    {
        Site = site ?? throw new ArgumentNullException(nameof(site));
        Language = language;
        Origin = origin.TrimEnd('/');
        Path = path;
    }

    public OperationsPublicSite Site { get; }

    public string Language { get; }

    public string Origin { get; }

    // The page address after the language, for example "" or "/locations".
    public string Path { get; }

    public string Company => Site.Settings.DisplayName;

    public string Url(string language) => Origin + "/" + language + Path;

    public string Home => "/" + Language;

    public string Text(IStringLocalizer<OperationsText> text, string key, string defaultKey)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Site.Text(key) is { Length: > 0 } own ? own : VixlyText.Format(text, defaultKey, "company", Company);
    }

    public string? Image(Guid? imageId) => imageId is { } id ? "/site/image/" + id.ToString("N") : null;
}
