using Vixly.Shared.Help;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: OperationsHelpPresenter.cs
//
// PURPOSE:
// Loads text-only help for the current company and staff reader.
//
// RESPONSIBILITIES:
// - Filter direct, search and related requests before rendering.
// - Encode company tokens without changing the singleton catalogue.
//
// IMPORTANT NOTES:
// - Contains no HTTP or Blazor types; scoped to one request.
// - A hidden slug is indistinguishable from an absent slug.
// =========================================================
public sealed class OperationsHelpPresenter : PresenterBase
{
    private readonly OperationsHelpCatalogue _help;
    private readonly IOperationsHelpReaderSource _reader;

    public OperationsHelpPresenter(OperationsHelpCatalogue help, IOperationsHelpReaderSource reader,
        ILogger<OperationsHelpPresenter> logger) : base(logger)
    {
        _help = help ?? throw new ArgumentNullException(nameof(help));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public OperationsHelpState State { get; private set; } = OperationsHelpState.Empty;

    public Task LoadAsync(string language, string? slug, string? query) => RunAsync(async _ =>
    {
        State = OperationsHelpState.Empty;
        OperationsHelpReaderContext context = await _reader.GetAsync();
        if (!context.Available) return;
        string term = (query ?? string.Empty).Trim();
        if (term.Length > 160) term = term[..160];
        HelpArticle? article = slug is null ? null : _help.Catalogue.Find(language, slug, context.Reader);
        IReadOnlyList<HelpArticle> articles = slug is not null ? [] : term.Length == 0
            ? _help.Catalogue.Visible(language, context.Reader) : HelpSearch.Find(_help.Catalogue, language, term, context.Reader);
        State = new(context.BrandName, article, article is null ? string.Empty : HelpRenderer.ReplaceTokens(article.Html, context.Tokens),
            articles, article is null ? [] : _help.Catalogue.Related(article, context.Reader), term, slug is not null && article is null, false);
    });
}
