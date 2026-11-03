using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing.Template;
using Vixly.Public.Web.Hosting;
using Vixly.Shared.Help;

namespace Vixly.Public.Web.Features.Help;

// =========================================================
// FILE: PublicHelpCatalogue.cs
//
// PURPOSE:
// Loads and validates Public's embedded help once at startup.
//
// RESPONSIBILITIES:
// - Supply Public's sections, routes, public reader and addresses.
// - Resolve contextual links without exposing visitor URL tokens.
//
// IMPORTANT NOTES:
// - Public help has no permissions, conditions or brand tokens.
// - HTTP tests also validate routes against EndpointDataSource.
// =========================================================
public sealed class PublicHelpCatalogue
{
    public static IReadOnlyList<string> Sections { get; } = Array.AsReadOnly(new[]
        { "getting-started", "sign-up", "paying", "terms-and-contact" });

    public static HelpReader Reader { get; } = new([], []);

    public PublicHelpCatalogue()
    {
        Assembly assembly = typeof(PublicHelpCatalogue).Assembly;
        Documents = HelpArticleSource.Read(assembly);
        HashSet<string> routes = assembly.GetTypes().SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template).ToHashSet(StringComparer.Ordinal);
        Catalogue = new(Documents, Options(routes));
    }

    public IReadOnlyList<HelpDocument> Documents { get; }
    public HelpCatalogue Catalogue { get; }

    public static HelpOptions Options(IReadOnlySet<string> routes) => new()
    {
        Sections = Sections.ToHashSet(StringComparer.Ordinal), Routes = routes,
        ArticleAddress = Address
    };

    public static string Home(string language) => PublicLanguage.Page(language, "help");
    public static string Address(string language, string slug) => PublicLanguage.Page(language, "help/" + slug);

    public string ContextAddress(string language, string path)
    {
        foreach (HelpArticle article in Catalogue.Visible(language, Reader))
        {
            foreach (string route in article.Metadata.Routes)
            {
                TemplateMatcher matcher = new(TemplateParser.Parse(route), new RouteValueDictionary());
                if (matcher.TryMatch(new PathString(path), new RouteValueDictionary())) return Address(language, article.Metadata.Slug);
            }
        }
        return Home(language);
    }
}
