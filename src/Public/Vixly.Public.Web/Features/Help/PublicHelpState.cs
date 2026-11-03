using Vixly.Shared.Help;

namespace Vixly.Public.Web.Features.Help;

// =========================================================
// FILE: PublicHelpState.cs
//
// PURPOSE:
// Captures one Public help browsing result.
//
// RESPONSIBILITIES:
// - Keep article and search state independent of Blazor.
//
// IMPORTANT NOTES:
// - Article HTML comes only from the checked catalogue.
// =========================================================
public sealed record PublicHelpState(HelpArticle? Article, IReadOnlyList<HelpArticle> Articles, string Query, bool NotFound)
{
    public static PublicHelpState Empty { get; } = new(null, [], string.Empty, false);
}
