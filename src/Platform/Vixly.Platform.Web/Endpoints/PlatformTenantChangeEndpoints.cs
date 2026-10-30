using Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChangeProof;
using Vixly.Platform.Web.Security;

namespace Vixly.Platform.Web.Endpoints;

// =========================================================
// FILE: PlatformTenantChangeEndpoints.cs
//
// PURPOSE:
// Serves a change request’s proof document to signed-in staff (B12).
//
// RESPONSIBILITIES:
// - Check the reader through the use case
// - Serve the file as an attachment with its own type
//
// IMPORTANT NOTES:
// - Never cached; nosniff, so a file is never run as another type
// =========================================================
public static class PlatformTenantChangeEndpoints
{
    public static IEndpointRouteBuilder MapPlatformTenantChangeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/tenants/changes/{requestId:guid}/proof", async (
            Guid requestId,
            HttpContext http,
            GetPlatformTenantChangeProofUseCase proofs,
            CancellationToken cancellationToken) =>
        {
            try
            {
                GetPlatformTenantChangeProofResponse response = await proofs.ExecuteAsync(
                    PlatformCurrentUser.FromPrincipal(http.User),
                    new GetPlatformTenantChangeProofRequest { RequestId = requestId },
                    cancellationToken);

                if (!response.Succeeded)
                {
                    return Results.NotFound();
                }

                http.Response.Headers.CacheControl = "no-store, private";
                http.Response.Headers.XContentTypeOptions = "nosniff";
                return Results.File(response.File!.Content, response.File.ContentType, response.File.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        }).RequireAuthorization();

        return endpoints;
    }
}
