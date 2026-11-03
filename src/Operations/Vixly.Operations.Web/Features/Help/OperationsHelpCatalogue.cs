using Vixly.Operations.Domain.Access;
using Vixly.Operations.Resources;
using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: OperationsHelpCatalogue.cs
//
// PURPOSE:
// Loads company-branded help against the real Operations routes.
//
// RESPONSIBILITIES:
// - Validate permissions, conditions and embedded translations.
//
// IMPORTANT NOTES:
// - Holds no tenant names, reader permissions or rendered tokens.
// =========================================================
public sealed class OperationsHelpCatalogue
{
    public static IReadOnlyList<string> Sections { get; } = Array.AsReadOnly(new[]
    {
        "getting-started", "setting-up", "receiving-and-checking", "at-the-counter", "money",
        "consolidation-and-dispatch", "at-destination", "team-and-company", "website-and-integrations", "reports"
    });

    public OperationsHelpCatalogue(IReadOnlySet<string> routes)
    {
        Documents = HelpArticleSource.Read(typeof(OperationsText).Assembly);
        Catalogue = new(Documents, Options(routes));
    }

    public IReadOnlyList<HelpDocument> Documents { get; }
    public HelpCatalogue Catalogue { get; }

    public static HelpOptions Options(IReadOnlySet<string> routes) => new()
    {
        Sections = Sections.ToHashSet(StringComparer.Ordinal), Routes = routes,
        Permissions = OperationsPermissions.All.ToHashSet(StringComparer.Ordinal),
        Conditions = new HashSet<string> { "partner_agents", "text_messages", "tenant_api", "website_theme", "website_content" },
        Tokens = new HashSet<string> { "brandName", "portalAddress" }, TenantBranded = true,
        ArticleAddress = Address
    };

    public static string Address(string language, string slug) => "/staff/help/" + slug;

    public string ContextAddress(string language, string? route, HelpReader reader) =>
        route is not null && Catalogue.ForRoute(language, route, reader) is { } article
            ? Address(language, article.Metadata.Slug) : "/staff/help";
}
