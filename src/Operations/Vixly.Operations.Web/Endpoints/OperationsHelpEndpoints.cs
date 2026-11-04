using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Vixly.Operations.Web.Components.Help;
using Vixly.Operations.Web.Features.Help;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsHelpEndpoints.cs
//
// PURPOSE:
// Routes signed-in staff and partner agents to filtered help.
//
// RESPONSIBILITIES:
// - Apply normal staff authorization and no-store responses.
// - Return the same 404 for missing and hidden article slugs.
//
// IMPORTANT NOTES:
// - The existing device and company-access middleware still apply.
// - Identity and company values are never accepted from a query.
// =========================================================
public static class OperationsHelpEndpoints
{
    public static IEndpointRouteBuilder MapOperationsHelpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        RouteGroupBuilder help = endpoints.MapGroup("/staff/help").RequireAuthorization();
        help.MapGet("", (HttpContext http, string? q, OperationsHelpPresenter presenter) => PageAsync(http, presenter, null, q));
        help.MapGet("/{Slug}", (HttpContext http, string Slug, OperationsHelpPresenter presenter) => PageAsync(http, presenter, Slug, null));
        return endpoints;
    }

    private static async Task<IResult> PageAsync(HttpContext http, OperationsHelpPresenter presenter, string? slug, string? query)
    {
        http.Response.Headers.CacheControl = "no-store";
        await presenter.LoadAsync(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, slug, query);
        return new RazorComponentResult<OperationsHelpPage>(new { State = presenter.State })
        {
            StatusCode = presenter.State.Unavailable ? StatusCodes.Status503ServiceUnavailable
                : presenter.State.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status200OK
        };
    }
}
