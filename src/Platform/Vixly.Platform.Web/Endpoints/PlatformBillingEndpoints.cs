using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingDocument;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Web.Security;

namespace Vixly.Platform.Web.Endpoints;

// =========================================================
// FILE: PlatformBillingEndpoints.cs
//
// PURPOSE:
// Download of stored billing documents (D-B10).
//
// RESPONSIBILITIES:
// - Return the stored PDF of an invoice, credit note or receipt to signed-in staff with billing access
//
// IMPORTANT NOTES:
// - The file is the one stored at issue; it is never regenerated
// - Not cached by the browser or proxies; refused requests reveal nothing about the document
// =========================================================
public static class PlatformBillingEndpoints
{
    public static IEndpointRouteBuilder MapPlatformBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/billing/documents/{kind}/{ownerId:guid}", async (
            string kind,
            Guid ownerId,
            HttpContext http,
            GetPlatformBillingDocumentUseCase documents,
            CancellationToken cancellationToken) =>
        {
            PlatformBillingDocumentKind? parsed = kind switch
            {
                "invoice" => PlatformBillingDocumentKind.Invoice,
                "credit_note" => PlatformBillingDocumentKind.CreditNote,
                "receipt" => PlatformBillingDocumentKind.Receipt,
                _ => null
            };

            if (parsed is null)
            {
                return Results.NotFound();
            }

            try
            {
                GetPlatformBillingDocumentResponse response = await documents.ExecuteAsync(
                    PlatformCurrentUser.FromPrincipal(http.User),
                    new GetPlatformBillingDocumentRequest { Kind = parsed.Value, OwnerId = ownerId },
                    cancellationToken);

                if (!response.Succeeded)
                {
                    return Results.NotFound();
                }

                http.Response.Headers.CacheControl = "no-store, private";
                return Results.File(response.Content, "application/pdf", response.FileName);
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
