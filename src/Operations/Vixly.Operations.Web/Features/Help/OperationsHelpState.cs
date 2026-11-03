using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: OperationsHelpState.cs
//
// PURPOSE:
// Holds permission-filtered, request-branded help presentation.
//
// RESPONSIBILITIES:
// - Keep hidden articles out of lists and related links.
//
// IMPORTANT NOTES:
// - HTML contains only validated catalogue output and encoded tokens.
// =========================================================
public sealed record OperationsHelpState(string Company, HelpArticle? Article, string Html,
    IReadOnlyList<HelpArticle> Articles, IReadOnlyList<HelpArticle> Related, string Query, bool NotFound, bool Unavailable)
{
    public static OperationsHelpState Empty { get; } = new(string.Empty, null, string.Empty, [], [], string.Empty, false, true);
}
