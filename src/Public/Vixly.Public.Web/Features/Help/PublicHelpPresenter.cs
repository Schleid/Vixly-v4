using Vixly.Shared.Help;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Public.Web.Features.Help;

// =========================================================
// FILE: PublicHelpPresenter.cs
//
// PURPOSE:
// Loads an article or searches Public help for anonymous readers.
//
// RESPONSIBILITIES:
// - Select the requested language, slug and bounded search query.
//
// IMPORTANT NOTES:
// - Public has no reader use case or authorization dependency.
// - No HTTP request, navigation or Blazor type belongs here.
// =========================================================
public sealed class PublicHelpPresenter : PresenterBase
{
    private readonly PublicHelpCatalogue _help;

    public PublicHelpPresenter(PublicHelpCatalogue help, ILogger<PublicHelpPresenter> logger) : base(logger)
    {
        _help = help ?? throw new ArgumentNullException(nameof(help));
    }

    public PublicHelpState State { get; private set; } = PublicHelpState.Empty;

    public Task LoadAsync(string language, string? slug, string? query) => RunAsync(_ =>
    {
        string term = (query ?? string.Empty).Trim();
        if (term.Length > 160) term = term[..160];
        HelpArticle? article = slug is null ? null : _help.Catalogue.Find(language, slug, PublicHelpCatalogue.Reader);
        IReadOnlyList<HelpArticle> articles = slug is not null ? [] : term.Length == 0
            ? _help.Catalogue.Visible(language, PublicHelpCatalogue.Reader)
            : HelpSearch.Find(_help.Catalogue, language, term, PublicHelpCatalogue.Reader);
        State = new(article, articles, term, slug is not null && article is null);
        return Task.CompletedTask;
    });
}
