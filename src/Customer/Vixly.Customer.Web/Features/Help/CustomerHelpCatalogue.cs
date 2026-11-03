using Vixly.Customer.Resources;
using Vixly.Shared.Help;

namespace Vixly.Customer.Web.Features.Help;

// =========================================================
// FILE: CustomerHelpCatalogue.cs
// PURPOSE:
// Publishes the three priority Customer Version 2 guides.
// RESPONSIBILITIES:
// - Validate routes, translations and local image addresses.
// IMPORTANT NOTES:
// - Contains no customer profiles, sessions or company names.
// =========================================================
public sealed class CustomerHelpCatalogue
{
    public CustomerHelpCatalogue() => Catalogue = new(HelpArticleSource.Read(typeof(CustomerText).Assembly), new()
    {
        Sections = new HashSet<string> { "getting-started", "your-packages", "shipping", "paying", "when-something-is-wrong", "your-account" },
        Routes = new HashSet<string> { "/customer/", "/customer/packages/new", "/customer/packages/shipments/new" },
        Tokens = new HashSet<string> { "brandName" }, Customer = true, TenantBranded = true,
        MediaPath = "/customer/help/media/", ArticleAddress = Address
    });

    public HelpCatalogue Catalogue { get; }
    public static string Address(string language, string slug) => "/customer/help/" + slug;
    public string ContextAddress(string language, string route, bool signedIn) =>
        Catalogue.ForRoute(language, route == "/customer" ? "/customer/" : route, new([], [], signedIn)) is { } article
            ? Address(language, article.Metadata.Slug) : "/customer/help";
}
