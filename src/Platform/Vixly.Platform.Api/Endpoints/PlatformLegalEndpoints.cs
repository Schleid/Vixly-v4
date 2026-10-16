using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Legal;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Application.UseCases.Legal.GetPublicLegalFile;
using Vixly.Platform.Application.UseCases.Legal.GetPublicLegalTerms;
using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformLegalEndpoints.cs
//
// PURPOSE:
// Public legal calls for the registration page (Slice 8, D-Lg9).
//
// RESPONSIBILITIES:
// - GET /legal/terms?language=xx: the current Terms with the documents it includes
// - GET /legal/documents/{id}/{language}: a published PDF
//
// IMPORTANT NOTES:
// - Anonymous: published documents are public; drafts are never served
// - PDFs are sent as attachments with nosniff, never rendered by the API
// =========================================================
public static class PlatformLegalEndpoints
{
    private static readonly PlatformAnonymousAuthorizationContext Anonymous = PlatformAnonymousAuthorizationContext.Instance;

    public static IEndpointRouteBuilder MapPlatformLegalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/legal/terms", async (
            string? language,
            GetPublicLegalTermsUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            GetPublicLegalTermsResponse response = await useCase.ExecuteAsync(Anonymous, new GetPublicLegalTermsRequest { Language = language }, cancellationToken);
            return response.Succeeded ? Results.Ok(Info(response.Terms!)) : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Catalogue);

        endpoints.MapGet("/legal/documents/{documentId:guid}/{language}", async (
            Guid documentId,
            string language,
            HttpContext http,
            GetPublicLegalFileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            GetPublicLegalFileResponse response = await useCase.ExecuteAsync(Anonymous, new GetPublicLegalFileRequest { DocumentId = documentId, Language = language }, cancellationToken);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            // A published version never changes, so it can be cached.
            http.Response.Headers.CacheControl = "public, max-age=3600";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.File!.Content, "application/pdf", $"{response.File.Kind}-{response.File.Version}-{response.File.Language}.pdf");
        }).RequireRateLimiting(PlatformApiRateLimits.Catalogue);

        return endpoints;
    }

    internal static PlatformLegalTermsInfo Info(PlatformLegalTerms terms)
    {
        return new PlatformLegalTermsInfo(
            terms.DocumentId,
            terms.Version,
            terms.Language,
            terms.Sha256,
            terms.Summary,
            terms.EffectiveUtc,
            FileUrl(terms.DocumentId, terms.Language),
            terms.Parts.Select(part => new PlatformLegalPartInfo(part.DocumentId, Kind(part.Kind), part.Version, part.Sha256, part.Summary, FileUrl(part.DocumentId, terms.Language))).ToArray());
    }

    private static string FileUrl(Guid documentId, string language) => $"/legal/documents/{documentId:D}/{language}";

    private static string Kind(PlatformLegalDocumentKind kind) => kind switch
    {
        PlatformLegalDocumentKind.Privacy => "privacy",
        PlatformLegalDocumentKind.DataProcessing => "data_processing",
        PlatformLegalDocumentKind.AcceptableUse => "acceptable_use",
        PlatformLegalDocumentKind.ServiceLevel => "service_level",
        _ => "terms"
    };
}
