using Microsoft.Extensions.Options;
using Vixly.Contracts.Operations.Internal;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Operations;

namespace Vixly.Customer.Web.Tenancy;

// =========================================================
// FILE: CustomerTenantMiddleware.cs
//
// PURPOSE:
// Finds the tenant of every customer request from its host name (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Ask Operations.Api which tenant the host belongs to
// - Send unknown hosts to 404 and suspended tenants to "not available"
//
// IMPORTANT NOTES:
// - Customers never see a tenant still being set up (Operations answers not found)
// - Operations unreachable gives a "try again" page (503)
// - The tenant comes only from the host (S-2); staff cookies are never read
// =========================================================
public sealed class CustomerTenantMiddleware
{
    public const string HostItem = "vx.customer.tenant";

    public const string NotFoundPath = "/not-found";

    public const string NotAvailablePath = "/not-available";

    public const string UnavailablePath = "/unavailable";

    private readonly RequestDelegate _next;

    public CustomerTenantMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext http, CustomerOperationsApi operations, IOptions<CustomerHostingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(options);

        PathString path = http.Request.Path;

        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_content") || Path.HasExtension(path.Value))
        {
            await _next(http);
            return;
        }

        string? slug = Slug(http.Request.Host.Host, options.Value);
        OperationsTenantHostDto? host = slug is null
            ? new OperationsTenantHostDto(false, null, "not_found", "none", null, null)
            : await operations.FindTenantAsync(slug, http.RequestAborted);

        if (host is null)
        {
            Show(http, UnavailablePath, StatusCodes.Status503ServiceUnavailable);
        }
        else
        {
            http.Items[HostItem] = host;

            if (!host.Found || host.State == "not_found")
            {
                Show(http, NotFoundPath, StatusCodes.Status404NotFound);
            }
            else if (host.State != "live")
            {
                Show(http, NotAvailablePath, StatusCodes.Status403Forbidden);
            }
        }

        await _next(http);
    }

    public static OperationsTenantHostDto? Host(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.Items[HostItem] as OperationsTenantHostDto;
    }

    // =====================================================
    // METHOD: SLUG FROM HOST
    // =====================================================
    public static string? Slug(string host, CustomerHostingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string name = (host ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();

        if (name.Length == 0)
        {
            return null;
        }

        if (name == "localhost")
        {
            return string.IsNullOrWhiteSpace(options.DevelopmentSlug) ? null : options.DevelopmentSlug.Trim().ToLowerInvariant();
        }

        foreach (string domain in options.BaseDomains.Where(domain => !string.IsNullOrWhiteSpace(domain)))
        {
            string suffix = "." + domain.Trim().TrimStart('.').ToLowerInvariant();

            if (name.EndsWith(suffix, StringComparison.Ordinal) && name[..^suffix.Length] is { Length: > 0 } label && !label.Contains('.', StringComparison.Ordinal))
            {
                string slug = !string.IsNullOrEmpty(options.SlugSuffix) && label.EndsWith(options.SlugSuffix, StringComparison.Ordinal) ? label[..^options.SlugSuffix.Length] : label;
                return slug.Length == 0 ? null : slug;
            }
        }

        return null;
    }

    // Serves the status page in place of the asked path. The status is set as the response starts:
    // a 404 set before rendering makes the component renderer skip the page body.
    // Any endpoint already chosen for the asked path is dropped (UsePathBase routes early), so routing picks the status page.
    private static void Show(HttpContext http, string page, int statusCode)
    {
        http.Request.Path = page;
        http.SetEndpoint(null);
        http.Request.RouteValues.Clear();
        http.Response.OnStarting(() =>
        {
            // Only a page that rendered normally takes the status; a refusal (429) or an error keeps its own.
            if (http.Response.StatusCode == StatusCodes.Status200OK)
            {
                http.Response.StatusCode = statusCode;
            }

            return Task.CompletedTask;
        });
    }
}
