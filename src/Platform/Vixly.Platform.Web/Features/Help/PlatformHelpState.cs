using Vixly.Shared.Help;

namespace Vixly.Platform.Web.Features.Help;

// =========================================================
// FILE: PlatformHelpState.cs
//
// PURPOSE:
// Holds the current reader's filtered help presentation.
//
// RESPONSIBILITIES:
// - Keep hidden articles out of all presentation collections.
//
// IMPORTANT NOTES:
// - Reader rules are refreshed for every navigation.
// =========================================================
public sealed record PlatformHelpState(
    HelpArticle? Article, IReadOnlyList<HelpArticle> Articles,
    IReadOnlyList<HelpArticle> Related, string Query, bool NotFound, string ContextAddress)
{
    public static PlatformHelpState Empty { get; } = new(null, [], [], string.Empty, false, "/help");
}
