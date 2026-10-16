using Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalFile;
using Vixly.Platform.Web.Security;

namespace Vixly.Platform.Web.Endpoints;

// =========================================================
// FILE: PlatformLegalEndpoints.cs
//
// PURPOSE:
// Staff download of a legal document file (Slice 8).
//
// RESPONSIBILITIES:
// - Serve the PDF of one document version in one language to staff with legal view
//
// IMPORTANT NOTES:
// - Signed-in staff only; drafts included (for review before publishing)
// - Sent as a download, never rendered inline
// =========================================================
public static class PlatformLegalEndpoints
{
    public static IEndpointRouteBuilder MapPlatformLegalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/legal/files/{documentId:guid}/{language}", async (
            Guid documentId,
            string language,
            HttpContext http,
            GetPlatformLegalFileUseCase files,
            CancellationToken cancellationToken) =>
        {
            try
            {
                GetPlatformLegalFileResponse response = await files.ExecuteAsync(
                    PlatformCurrentUser.FromPrincipal(http.User),
                    new GetPlatformLegalFileRequest { DocumentId = documentId, Language = language },
                    cancellationToken);

                if (!response.Succeeded)
                {
                    return Results.NotFound();
                }

                http.Response.Headers.CacheControl = "no-store, private";
                http.Response.Headers.XContentTypeOptions = "nosniff";
                return Results.File(response.File!.Content, "application/pdf", $"{response.File.Kind}-{response.File.Version}-{response.File.Language}.pdf");
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
