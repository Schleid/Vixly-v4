using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpRenderer.cs
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
public sealed class HelpRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml().UsePipeTables().UseAutoIdentifiers().Build();
    private static readonly MarkdownPipeline InspectionPipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();

    public static IReadOnlyList<string> Inspect(string body, HelpOptions options)
    {
        List<string> problems = [];
        MarkdownDocument document = Markdown.Parse(body, InspectionPipeline);
        if (document.Descendants<HtmlBlock>().Any() || document.Descendants<HtmlInline>().Any())
            problems.Add("Raw HTML is forbidden.");
        if (document.Descendants<HeadingBlock>().Any(heading => heading.Level < 2))
            problems.Add("Article headings must start at level two.");
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            if (!AllowedLink(link.Url ?? "", link.IsImage, options)) problems.Add("Forbidden link.");
        }
        foreach (LiteralInline literal in document.Descendants<LiteralInline>())
        {
            foreach (Match token in Regex.Matches(literal.Content.ToString(), @"\{(\w+)\}"))
                if (!options.Tokens.Contains(token.Groups[1].Value)) problems.Add("Unknown token.");
        }
        return problems.AsReadOnly();
    }

    public string Render(string body, string language, HelpOptions options)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(options);
        // Raw HTML remains escaped when rendering directly; HelpCheck rejects it at publication.
        if (Inspect(body, options).Any(problem => problem != "Raw HTML is forbidden."))
            throw new FormatException("Unsafe article Markdown.");
        MarkdownDocument document = Markdown.Parse(body, Pipeline);
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            if (link.Url?.StartsWith("help:", StringComparison.Ordinal) == true)
            {
                string address = options.ArticleAddress(language, link.Url[5..]);
                if (!AllowedLink(address, false, options) || address.StartsWith("help:", StringComparison.Ordinal))
                    throw new FormatException("Invalid help address.");
                link.Url = address;
            }
        }
        using StringWriter writer = new();
        Markdig.Renderers.HtmlRenderer renderer = new(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        return writer.ToString();
    }

    public static string ReplaceTokens(string html, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(values);
        // Replacement uses a single pass, so token-like text in a company name is never expanded again.
        return Regex.Replace(html, @"<code\b[^>]*>[\s\S]*?</code>|<[^>]+>|(?<text>[^<]+)", segment =>
            !segment.Groups["text"].Success ? segment.Value :
            Regex.Replace(segment.Value, @"\{(brandName|portalAddress)\}", match =>
                values.TryGetValue(match.Groups[1].Value, out string? value)
                    ? WebUtility.HtmlEncode(value) : throw new FormatException("Missing help token value.")));
    }

    public static string PlainText(string body) => Markdown.ToPlainText(body, Pipeline);

    internal static IEnumerable<string> LinkedSlugs(string body) =>
        Markdown.Parse(body, InspectionPipeline).Descendants<LinkInline>()
            .Where(link => link.Url?.StartsWith("help:", StringComparison.Ordinal) == true)
            .Select(link => link.Url![5..]);

    internal static (int Sections, int Steps) Structure(string body)
    {
        MarkdownDocument document = Markdown.Parse(body, InspectionPipeline);
        return (document.Descendants<HeadingBlock>().Count(heading => heading.Level == 2),
            document.Descendants<ListBlock>().Where(list => list.IsOrdered).Sum(list => list.Count));
    }

    private static bool AllowedLink(string url, bool image, HelpOptions options)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Any(char.IsControl) || url.Contains('\\') || Regex.IsMatch(url, @"\{\w+\}")) return false;
        string decoded = Uri.UnescapeDataString(WebUtility.HtmlDecode(url));
        if (decoded.StartsWith("//", StringComparison.Ordinal) || decoded.Contains('\\') || decoded.Any(char.IsControl)) return false;
        if (image) return decoded.StartsWith(options.MediaPath, StringComparison.Ordinal)
            && !decoded.Split('/').Any(part => part is ".." or ".");
        if (decoded.StartsWith("help:", StringComparison.Ordinal))
            return Regex.IsMatch(decoded[5..], "^[a-z0-9]+(?:-[a-z0-9]+)*$");
        // A path from the root is relative on every system (Linux would otherwise read it as a file address).
        if (!decoded.StartsWith('/') && Uri.TryCreate(decoded, UriKind.Absolute, out Uri? absolute)) return absolute.Scheme == "https";
        return !decoded.Split('/', '?', '#')[0].Contains(':');
    }
}
