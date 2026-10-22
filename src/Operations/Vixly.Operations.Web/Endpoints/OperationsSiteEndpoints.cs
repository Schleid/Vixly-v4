using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.HttpResults;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Application.UseCases.Site.GetOperationsPublicPricing;
using Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;
using Vixly.Operations.Application.UseCases.Site.GetOperationsSiteImage;
using Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;
using Vixly.Operations.Web.Components.Site;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Site;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsSiteEndpoints.cs
//
// PURPOSE:
// The tenant public site: home, locations, tracking, images, robots and sitemap (Slice 1B).
//
// RESPONSIBILITIES:
// - / goes to the visitor language; /{language} pages in that language
// - Serve published images with long caching
// - Answer robots.txt and sitemap.xml for the tenant only
//
// IMPORTANT NOTES:
// - Everything describes the tenant, never Vixly
// - The draft shows only to the signed-in Owner with ?preview=1, never cached
// - Tracking shows milestones only (ADR-0021) and is rate limited
// =========================================================
public static class OperationsSiteEndpoints
{
    private const string Language = "{language:regex(^(en|fr|es)$)}";

    public static IEndpointRouteBuilder MapOperationsSiteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/", () =>
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Results.Redirect("/" + (OperationsSiteLanguages.IsSiteLanguage(language) ? language : OperationsSiteLanguages.All[0]));
        });

        endpoints.MapGet("/" + Language, (HttpContext http, string language, bool? preview, GetOperationsPublicSiteUseCase useCase, GetOperationsPublicPricingUseCase pricing) =>
            RenderAsync<SiteHomePage>(http, language, string.Empty, preview == true, useCase, pricing, null));

        endpoints.MapGet("/" + Language + "/locations", (HttpContext http, string language, bool? preview, GetOperationsPublicSiteUseCase useCase, GetOperationsPublicPricingUseCase pricing) =>
            RenderAsync<SiteLocationsPage>(http, language, "/locations", preview == true, useCase, pricing, null));

        // The prices the company publishes (owner, 2026-10-21); not found while it does not.
        endpoints.MapGet("/" + Language + "/pricing", (HttpContext http, string language, bool? preview, GetOperationsPublicSiteUseCase useCase, GetOperationsPublicPricingUseCase pricing) =>
            RenderAsync<SitePricingPage>(http, language, "/pricing", preview == true, useCase, pricing, null, true));

        endpoints.MapGet("/" + Language + "/track", async (HttpContext http, string language, string? number, GetOperationsPublicSiteUseCase useCase, GetOperationsPublicPricingUseCase pricing,
            TrackOperationsShipmentPubliclyUseCase track) =>
        {
            TrackOperationsShipmentPubliclyResponse? result = string.IsNullOrWhiteSpace(number)
                ? null
                : await track.ExecuteAsync(OperationsCurrentUser.Context(http), new TrackOperationsShipmentPubliclyRequest { Number = number.Trim() }, http.RequestAborted);

            return await RenderAsync<SiteTrackPage>(http, language, "/track", false, useCase, pricing, new Dictionary<string, object?>
            {
                ["Number"] = number?.Trim(),
                ["Milestones"] = result is { Succeeded: true } ? result.Milestones : null,
                ["NotFound"] = result is { Succeeded: false }
            });
        }).RequireRateLimiting(OperationsRateLimits.Tracking);

        endpoints.MapGet("/site/image/{imageId:guid}", async (HttpContext http, Guid imageId, GetOperationsSiteImageUseCase useCase) =>
        {
            GetOperationsSiteImageResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSiteImageRequest { ImageId = imageId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = response.IsPublished ? "public, max-age=31536000, immutable" : "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentDisposition = "inline";
            return Results.Bytes(response.Content, response.ContentType, entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"" + response.Sha256 + "\""));
        });

        endpoints.MapGet("/robots.txt", (HttpContext http) =>
        {
            bool live = OperationsTenantMiddlewareState.IsLive(http);
            string origin = Origin(http);
            string body = live
                ? $"User-agent: *\nAllow: /\nDisallow: /staff\nDisallow: /setup\nDisallow: /invitation\nDisallow: /customer\nDisallow: /site/\nSitemap: {origin}/sitemap.xml\n"
                : "User-agent: *\nDisallow: /\n";
            return Results.Text(body, "text/plain", Encoding.UTF8);
        });

        endpoints.MapGet("/sitemap.xml", async (HttpContext http, GetOperationsPublicPricingUseCase pricing) =>
        {
            GetOperationsPublicPricingResponse prices = await pricing.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPublicPricingRequest(), http.RequestAborted);
            string origin = Origin(http);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";
            XElement urls = new(ns + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", xhtml));

            foreach (string path in prices.Shown ? new[] { string.Empty, "/locations", "/pricing" } : new[] { string.Empty, "/locations" })
            {
                foreach (string language in OperationsSiteLanguages.All)
                {
                    urls.Add(new XElement(ns + "url",
                        new XElement(ns + "loc", origin + "/" + language + path),
                        OperationsSiteLanguages.All.Select(alternate => new XElement(xhtml + "link",
                            new XAttribute("rel", "alternate"), new XAttribute("hreflang", alternate), new XAttribute("href", origin + "/" + alternate + path)))));
                }
            }

            return Results.Text(new XDocument(new XDeclaration("1.0", "utf-8", null), urls).Declaration + "\n" + urls, "application/xml", Encoding.UTF8);
        });

        return endpoints;
    }

    // The Pricing page reads the whole price list; every other page asks only whether the menu shows Pricing (review of 94e28f4).
    private static async Task<IResult> RenderAsync<TPage>(HttpContext http, string language, string path, bool preview, GetOperationsPublicSiteUseCase useCase,
        GetOperationsPublicPricingUseCase pricing, Dictionary<string, object?>? extra, bool pricingPage = false)
        where TPage : Microsoft.AspNetCore.Components.IComponent
    {
        GetOperationsPublicSiteResponse response = await useCase.ExecuteAsync(
            OperationsCurrentUser.Context(http), new GetOperationsPublicSiteRequest { Language = language, Preview = preview }, http.RequestAborted);

        if (!response.Succeeded || response.Site is null)
        {
            return Results.NotFound();
        }

        GetOperationsPublicPricingResponse prices = await pricing.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPublicPricingRequest { Full = pricingPage }, http.RequestAborted);

        if (pricingPage && !prices.Shown)
        {
            return Results.NotFound();
        }

        http.Response.Headers.CacheControl = response.Site.IsPreview ? "private, no-store" : "public, max-age=60";
        Dictionary<string, object?> parameters = extra ?? new Dictionary<string, object?>();
        parameters["Page"] = new OperationsSitePage(response.Site with { PricingShown = prices.Shown, Pricing = prices.Pricing }, language, Origin(http), path);

        return new RazorComponentResult<TPage>(parameters);
    }

    private static string Origin(HttpContext http) => "https://" + http.Request.Host.Value;
}
