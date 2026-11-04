using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsBlankFields.cs
//
// PURPOSE:
// Treats an empty field of a staff page or form as a field not sent, so "All warehouses" and other empty choices never fail binding.
//
// RESPONSIBILITIES:
// - Drop empty values from the query string of /staff requests
// - Drop empty fields from url-encoded /staff forms
//
// IMPORTANT NOTES:
// - A browser sends an empty select or hidden input as "name="; minimal APIs refuse "" for a Guid?, int? or bool? (400, or an exception in Development)
// - Only /staff: webhooks and the public API keep their requests exactly as sent (a provider's signature covers every field)
// - Multipart forms are left alone: they are read later with the endpoint's own upload limits
// - A form whose antiforgery token failed is left alone: reading it would throw, and the form guard answers 400
// =========================================================
public sealed class OperationsBlankFields
{
    private readonly RequestDelegate _next;

    public OperationsBlankFields(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        if (http.Request.Path.StartsWithSegments("/staff", StringComparison.OrdinalIgnoreCase))
        {
            DropFromQuery(http.Request);
            await DropFromFormAsync(http);
        }

        await _next(http);
    }

    private static void DropFromQuery(HttpRequest request)
    {
        if (!request.Query.Any(pair => Blank(pair.Value)))
        {
            return;
        }

        request.QueryString = QueryString.Create(request.Query.Where(pair => !Blank(pair.Value)));
    }

    private static async Task DropFromFormAsync(HttpContext http)
    {
        if (!HttpMethods.IsPost(http.Request.Method)
            || !(http.Request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ?? false)
            || http.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false })
        {
            return;
        }

        IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);

        if (!form.Any(pair => Blank(pair.Value)))
        {
            return;
        }

        Dictionary<string, StringValues> kept = form.Where(pair => !Blank(pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        http.Features.Set<IFormFeature>(new FormFeature(new FormCollection(kept)));
    }

    // Every value empty: a list such as "a=&a=x" keeps its non-empty values as sent.
    private static bool Blank(StringValues values) => values.All(string.IsNullOrEmpty);
}
