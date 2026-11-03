using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing.Template;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Resources;
using Vixly.Shared.Help;

namespace Vixly.Platform.Web.Features.Help;

// =========================================================
// FILE: PlatformHelpCatalogue.cs
//
// PURPOSE:
// Loads Control's embedded help and resolves visible page help.
//
// RESPONSIBILITIES:
// - Validate sections, permissions and actual route templates.
//
// IMPORTANT NOTES:
// - The superuser pseudo-permission is never a role grant.
// - Every lookup requires the current reader's rules.
// =========================================================
public sealed class PlatformHelpCatalogue
{
    public static IReadOnlyList<string> Sections { get; } = Array.AsReadOnly(new[]
    {
        "getting-started", "companies", "commercial", "billing-and-payments",
        "licences-and-devices", "legal", "team-and-security", "settings"
    });

    public PlatformHelpCatalogue()
    {
        Documents = HelpArticleSource.Read(typeof(PlatformText).Assembly);
        HashSet<string> routes = typeof(PlatformHelpCatalogue).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template).ToHashSet(StringComparer.Ordinal);
        Catalogue = new(Documents, Options(routes));
    }

    public IReadOnlyList<HelpDocument> Documents { get; }
    public HelpCatalogue Catalogue { get; }

    public static HelpOptions Options(IReadOnlySet<string> routes) => new()
    {
        Sections = Sections.ToHashSet(StringComparer.Ordinal), Routes = routes,
        Permissions = PlatformPermissions.All.Append("superuser").ToHashSet(StringComparer.Ordinal),
        ArticleAddress = Address
    };

    public static string Address(string language, string slug) => "/help/" + slug;

    public string ContextAddress(string language, string path, HelpReader reader)
    {
        foreach (HelpArticle article in Catalogue.Visible(language, reader))
        {
            foreach (string route in article.Metadata.Routes)
            {
                RouteTemplate template = TemplateParser.Parse(route);
                TemplateMatcher matcher = new(template, new RouteValueDictionary());
                RouteValueDictionary values = new();
                if (matcher.TryMatch(new PathString(path), values)
                    && template.Parameters.All(parameter => !parameter.InlineConstraints.Any(constraint => constraint.Constraint == "guid")
                        || Guid.TryParse(values[parameter.Name!]?.ToString(), out _)))
                    return Address(language, article.Metadata.Slug);
            }
        }
        return "/help";
    }
}
