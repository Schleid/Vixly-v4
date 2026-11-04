using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Features.Help;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;
using Vixly.Shared.Help;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerHelpEndpoints.cs
// PURPOSE:
// Serves customer-owned help under the normal portal tenant gate.
// RESPONSIBILITIES:
// - Verify sessions through Operations and filter before rendering.
// IMPORTANT NOTES:
// - A cookie alone is never proof of a valid customer session.
// - Customer data and branded HTML are never cached.
// =========================================================
public static class CustomerHelpEndpoints
{
    public static IEndpointRouteBuilder MapCustomerHelpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/help", (HttpContext http, CustomerOperationsApi api, CustomerHelpCatalogue help, string? q) => PageAsync(http, api, help, null, q));
        endpoints.MapGet("/help/{slug}", (HttpContext http, CustomerOperationsApi api, CustomerHelpCatalogue help, string slug) => PageAsync(http, api, help, slug, null));
        return endpoints;
    }

    private static async Task<IResult> PageAsync(HttpContext http, CustomerOperationsApi api, CustomerHelpCatalogue help, string? slug, string? query)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant) return Results.NotFound();
        bool signedIn = false;
        if (CustomerSessionCookie.Read(http.Request) is { } session)
        {
            var me = await api.GetMeAsync(tenant, session, http, http.RequestAborted);
            signedIn = me.Value is not null;
            if (!signedIn)
            {
                if (me.ErrorCode == "ops.customers.session_required") CustomerSessionCookie.Delete(http.Response);
                else return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
        if (!signedIn && slug is not null) return Results.Redirect("/customer/sign-in");
        var brand = await api.GetBrandAsync(tenant, http.RequestAborted);
        if (string.IsNullOrWhiteSpace(brand?.DisplayName)) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        HelpReader reader = new([], [], signedIn);
        HelpArticle? article = slug is null ? null : help.Catalogue.Find(language, slug, reader);
        string term = (query ?? string.Empty).Trim();
        if (term.Length > 160) term = term[..160];
        var articles = slug is not null ? [] : term.Length == 0 ? help.Catalogue.Visible(language, reader) : HelpSearch.Find(help.Catalogue, language, term, reader);
        string html = article is null ? string.Empty : HelpRenderer.ReplaceTokens(article.Html, new Dictionary<string, string> { ["brandName"] = brand.DisplayName });
        CustomerHelpState state = new(brand, signedIn, article, html, articles, article is null ? [] : help.Catalogue.Related(article, reader), term, slug is not null && article is null);
        return new RazorComponentResult<CustomerHelpPage>(new { State = state }) { StatusCode = state.NotFound ? 404 : 200 };
    }
}
