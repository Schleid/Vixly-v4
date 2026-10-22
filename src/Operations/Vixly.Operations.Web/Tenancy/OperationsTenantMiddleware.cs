using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tenancy;

// =========================================================
// FILE: OperationsTenantMiddleware.cs
//
// PURPOSE:
// Finds the tenant of every request from its host name (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Resolve the host through Platform (cached)
// - Enter the tenant scope of the request
// - Send unknown hosts to 404, suspended tenants to "not available", tenants being set up to the setup pages
//
// IMPORTANT NOTES:
// - Health checks and static files skip it
// - Platform unreachable gives a "try again" page (503), never a 404
// - The tenant comes only from the host, never from a cookie or form (S-2)
// =========================================================
public sealed class OperationsTenantMiddleware
{
    public const string HostItem = "vx.ops.tenant";

    public const string NotFoundPath = "/not-found";

    public const string NotAvailablePath = "/not-available";

    public const string UnavailablePath = "/unavailable";

    public const string SetupPath = "/setup";

    // While a tenant is being set up, only the owner setup pages and their sign-in steps answer (Slice 1, D-O3).
    private static readonly string[] SetupPaths = { SetupPath, "/invitation", "/staff", "/devices", "/culture", "/session", "/robots.txt" };

    private readonly RequestDelegate _next;

    public OperationsTenantMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext http, ResolveOperationsTenantUseCase resolve, IOperationsTenantScope scope, IOptions<OperationsHostingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(resolve);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(options);

        PathString path = http.Request.Path;

        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_content") || (Path.HasExtension(path.Value) && !IsTenantFile(path)))
        {
            await _next(http);
            return;
        }

        string? slug = OperationsHostSlug.From(http.Request.Host.Host, options.Value);
        ResolveOperationsTenantResponse response = slug is null
            ? ResolveOperationsTenantResponse.Success(OperationsTenantHost.NotFound)
            : await resolve.ExecuteAsync(OperationsAuthorizationContext.Host, new ResolveOperationsTenantRequest { Slug = slug }, http.RequestAborted);

        if (!response.Succeeded)
        {
            Show(http, UnavailablePath, StatusCodes.Status503ServiceUnavailable);
            await _next(http);
            return;
        }

        OperationsTenantHost host = response.Host!;
        http.Items[HostItem] = host;

        if (host.TenantId is { } tenantId)
        {
            scope.Enter(tenantId);
        }

        switch (host.State)
        {
            case OperationsTenantState.NotFound:
                Show(http, NotFoundPath, StatusCodes.Status404NotFound);
                break;
            case OperationsTenantState.Suspended:
                Show(http, NotAvailablePath, StatusCodes.Status403Forbidden);
                break;
            case OperationsTenantState.SettingUp when !SetupPaths.Any(allowed => path.StartsWithSegments(allowed)):
                http.Response.Redirect(SetupPath);
                return;
            case OperationsTenantState.Live when path.StartsWithSegments(SetupPath):
                http.Response.Redirect("/");
                return;
        }

        await _next(http);
    }

    // robots.txt and sitemap.xml belong to the tenant site (Slice 1B), unlike other files.
    private static bool IsTenantFile(PathString path) => path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) || path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase);

    public static OperationsTenantHost? Host(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.Items[HostItem] as OperationsTenantHost;
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
