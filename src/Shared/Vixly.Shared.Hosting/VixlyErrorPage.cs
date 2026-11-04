using System.Text.Encodings.Web;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyErrorPage.cs
//
// PURPOSE:
// The page shown when a request fails unexpectedly, for every method (2026-10-18).
//
// RESPONSIBILITIES:
// - Answer /error for GET and POST alike, with status 500 and a way back
//
// IMPORTANT NOTES:
// - The exception handler re-runs the failed request here with its own method, so a failed form post must not meet a GET-only page (it showed a blank 404 or 405)
// - Plain HTML with no script and no form, so it can never fail itself; nothing about the error is shown
// =========================================================
public static class VixlyErrorPage
{
    public const string Path = "/error";

    public static IEndpointConventionBuilder MapVixlyErrorPage(this IEndpointRouteBuilder endpoints, Func<HttpContext, VixlyErrorPageText> text, string stylesheet)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(text);

        return endpoints.Map(Path, (HttpContext http) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            return Results.Content(Html(text(http), stylesheet), "text/html; charset=utf-8", Encoding.UTF8, StatusCodes.Status500InternalServerError);
        });
    }

    public static string Html(VixlyErrorPageText text, string stylesheet)
    {
        ArgumentNullException.ThrowIfNull(text);
        HtmlEncoder e = HtmlEncoder.Default;

        return "<!DOCTYPE html><html lang=\"" + e.Encode(text.Language) + "\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />"
            + "<meta name=\"robots\" content=\"noindex\" /><title>" + e.Encode(text.Title) + "</title>"
            + "<link rel=\"stylesheet\" href=\"/_content/Vixly.Shared.UI/vixly-theme.css\" /><link rel=\"stylesheet\" href=\"" + e.Encode(stylesheet) + "\" /></head>"
            + "<body><main class=\"vx-error\"><h1>" + e.Encode(text.Title) + "</h1><p>" + e.Encode(text.Body) + "</p><p><a href=\"" + e.Encode(text.BackUrl) + "\">" + e.Encode(text.Back) + "</a></p></main></body></html>";
    }
}
