using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Leads;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Leads.SubmitPlatformSalesLead;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformLeadEndpoints.cs
//
// PURPOSE:
// The public contact form endpoint (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Map POST /leads
//
// IMPORTANT NOTES:
// - Anonymous: bot check in the use case, 5 per hour per visitor IP (S-6)
// - Answers 204 with no body; the visitor is never emailed
// =========================================================
public static class PlatformLeadEndpoints
{
    public static IEndpointRouteBuilder MapPlatformLeadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/leads", async (PlatformSalesLeadBody body, SubmitPlatformSalesLeadUseCase useCase, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformSalesLeadKind>(body.Kind) is not { } kind)
            {
                return errors.Problem(PlatformSalesLeadErrors.KindInvalid);
            }

            SubmitPlatformSalesLeadResponse response = await useCase.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new SubmitPlatformSalesLeadRequest
            {
                CompanyName = body.CompanyName,
                ContactName = body.ContactName,
                Email = body.Email,
                Phone = body.Phone,
                CountryCode = body.CountryCode,
                Kind = kind,
                Message = body.Message,
                Language = body.Language,
                SourcePage = body.SourcePage,
                BotToken = body.BotToken
            }, cancellationToken);

            return response.Succeeded ? Results.NoContent() : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Leads);

        return endpoints;
    }
}
