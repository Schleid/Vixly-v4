using System.Text;
using Microsoft.Extensions.Options;
using Vixly.Public.Web.Hosting;
using Vixly.Public.Web.Platform;

namespace Vixly.Public.Web.Endpoints;

// =========================================================
// FILE: PublicEndpoints.cs
//
// PURPOSE:
// Non-page addresses of the public site: robots, sitemap, legal PDFs and the Zelle QR image (Stage 2).
//
// RESPONSIBILITIES:
// - Tell search engines what to index
// - Pass legal PDFs and the Zelle QR through from Platform.Api on the same origin
//
// IMPORTANT NOTES:
// - Sign-up, pay and card pages are disallowed for crawlers
// - Pass-through keeps the CSP to self and hides the API address
// =========================================================
public static class PublicEndpoints
{
    private static readonly string[] Pages = { string.Empty, "features", "plans", "contact", "legal" };

    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/robots.txt", (IOptions<PublicSiteOptions> site) => Results.Text(
            "User-agent: *\nDisallow: /pay/\nDisallow: /cards/\nDisallow: /register/\n"
            + string.Concat(PublicLanguage.Codes.Select(language => $"Disallow: /{language}/register\n"))
            + $"Sitemap: {site.Value.CanonicalBaseUrl.TrimEnd('/')}/sitemap.xml\n", "text/plain"));

        endpoints.MapGet("/sitemap.xml", (IOptions<PublicSiteOptions> site) =>
        {
            string root = site.Value.CanonicalBaseUrl.TrimEnd('/');
            StringBuilder xml = new("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">\n");

            foreach (string page in Pages)
            {
                foreach (string language in PublicLanguage.Codes)
                {
                    xml.Append("  <url><loc>").Append(root).Append(PublicLanguage.Page(language, page)).Append("</loc>");

                    foreach (string alternate in PublicLanguage.Codes)
                    {
                        xml.Append("<xhtml:link rel=\"alternate\" hreflang=\"").Append(alternate).Append("\" href=\"").Append(root).Append(PublicLanguage.Page(alternate, page)).Append("\"/>");
                    }

                    xml.Append("</url>\n");
                }
            }

            return Results.Text(xml.Append("</urlset>\n").ToString(), "application/xml");
        });

        endpoints.MapGet("/legal/documents/{documentId:guid}/{language}", async (Guid documentId, string language, HttpContext http, PlatformApi api, CancellationToken cancellationToken) =>
        {
            if (!PublicLanguage.Codes.Contains(language, StringComparer.Ordinal))
            {
                return Results.NotFound();
            }

            using HttpResponseMessage response = await api.LegalFileAsync(documentId, language, cancellationToken);
            return await PassAsync(response, http, "application/pdf", "public, max-age=3600", cancellationToken);
        });

        endpoints.MapGet("/pay/zelle/qr", async (int v, HttpContext http, PlatformApi api, CancellationToken cancellationToken) =>
        {
            using HttpResponseMessage response = await api.ZelleQrAsync(v, cancellationToken);
            return await PassAsync(response, http, response.Content.Headers.ContentType?.MediaType ?? "image/png", "public, max-age=86400", cancellationToken);
        });

        return endpoints;
    }

    private static async Task<IResult> PassAsync(HttpResponseMessage response, HttpContext http, string contentType, string cache, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Results.NotFound();
        }

        byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        http.Response.Headers.CacheControl = cache;
        http.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(content, contentType, response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }
}
