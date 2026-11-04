using Microsoft.AspNetCore.Http.HttpResults;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsStaffResults.cs
//
// PURPOSE:
// Shared answers of the Slice 3B staff endpoints (pages, files, the company name).
//
// RESPONSIBILITIES:
// - Render a page with no-store; send a file with no-store and nosniff; read the company name and the portal host
//
// IMPORTANT NOTES:
// - Endpoints of earlier slices keep their own copies; new endpoints use these
// =========================================================
public static class OperationsStaffResults
{
    public static IResult Page<TComponent>(HttpContext http, object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Response.Headers.CacheControl = "no-store";
        return new RazorComponentResult<TComponent>(parameters);
    }

    public static IResult File(HttpContext http, byte[] content, string contentType, string? fileName)
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Response.Headers.CacheControl = "private, no-store";
        http.Response.Headers.XContentTypeOptions = "nosniff";

        if (fileName is not null)
        {
            http.Response.Headers.ContentDisposition = "inline; filename=\"" + fileName + "\"";
        }

        return Results.Bytes(content, contentType);
    }

    public static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand)
    {
        ArgumentNullException.ThrowIfNull(brand);
        return (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
    }

    public static string PortalSlug(HttpContext http) => OperationsTenantMiddleware.Host(http)?.CanonicalSlug ?? string.Empty;
}
