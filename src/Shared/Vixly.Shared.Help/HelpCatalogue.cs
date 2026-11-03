using System.Collections.Frozen;

namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpCatalogue.cs
//
// PURPOSE:
// Implements shared help rendering, lookup or publication checks.
//
// RESPONSIBILITIES:
// - Enforce the help foundation contract of ADR-0023.
//
// IMPORTANT NOTES:
// - Applications supply trusted reader rules and validation catalogues.
// =========================================================
public sealed class HelpCatalogue
{
    private readonly FrozenDictionary<(string Language, string Slug), HelpArticle> articles;

    public HelpCatalogue(IEnumerable<HelpDocument> documents, HelpOptions options)
    {
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(options);
        HelpDocument[] sources = documents.ToArray();
        IReadOnlyList<string> problems = HelpCheck.Run(sources, options);
        if (problems.Count != 0) throw new FormatException(string.Join(Environment.NewLine, problems));
        HelpRenderer renderer = new();
        articles = sources.Select(source =>
        {
            (HelpFrontMatter metadata, string body) = HelpFrontMatter.Parse(source.Markdown, source.Language, options);
            return new HelpArticle(source.Language, source.Path, metadata, body,
                renderer.Render(body, source.Language, options), HelpRenderer.PlainText(body));
        }).ToFrozenDictionary(article => (article.Language, article.Metadata.Slug));
    }

    public HelpArticle? Find(string language, string slug, HelpReader reader) =>
        articles.TryGetValue((language, slug), out HelpArticle? article) && reader.CanRead(article) ? article : null;

    public HelpArticle? ForRoute(string language, string routeTemplate, HelpReader reader) =>
        Visible(language, reader).FirstOrDefault(article => article.Metadata.Routes.Contains(routeTemplate, StringComparer.Ordinal));

    public IReadOnlyList<HelpArticle> Visible(string language, HelpReader reader) =>
        articles.Values.Where(article => article.Language == language && reader.CanRead(article))
            .OrderBy(article => article.Metadata.Section, StringComparer.Ordinal).ThenBy(article => article.Metadata.Order)
            .ThenBy(article => article.Metadata.Slug, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<HelpArticle> Related(HelpArticle article, HelpReader reader) =>
        article.Metadata.Related.Select(slug => Find(article.Language, slug, reader)).OfType<HelpArticle>().ToArray();
}
