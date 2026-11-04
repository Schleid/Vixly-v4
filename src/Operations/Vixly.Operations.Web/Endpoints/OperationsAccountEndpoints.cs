using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.UseCases.Account.CompleteOperationsSecondFactorReset;
using Vixly.Operations.Application.UseCases.Legal.AcceptOperationsLegalTerms;
using Vixly.Operations.Application.UseCases.Legal.GetOperationsLegalStatus;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Web.Components.Editor;
using Vixly.Operations.Web.Components.SignIn;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsAccountEndpoints.cs
//
// PURPOSE:
// The Terms page, the read-only page and the second-factor reset sent by support (Slice 1C).
//
// RESPONSIBILITIES:
// - /legal and /staff/legal: read the Terms and accept them (Owner)
// - /staff/legal/documents: the published PDFs, served from the tenant host
// - /staff/limited: why a change was refused
// - /account/second-factor-reset: complete the reset support sent (D-O13)
//
// IMPORTANT NOTES:
// - Static forms with antiforgery (ADR-0019)
// - Platform checks the Owner role and the version (Slice 8)
// =========================================================
public static class OperationsAccountEndpoints
{
    public static IEndpointRouteBuilder MapOperationsAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Platform's emails link to /legal on the tenant host (Slice 8).
        endpoints.MapGet("/legal", () => Results.Redirect("/staff/legal"));

        endpoints.MapGet("/staff/legal", async (HttpContext http, string? done, GetOperationsLegalStatusUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text,
            Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText> texts) =>
            await LegalPageAsync(http, useCase, await CompanyAsync(http, brand), text, null, done is null ? null : texts["ui.legal.accepted"].Value)).RequireAuthorization();

        endpoints.MapPost("/staff/legal/accept", async (HttpContext http, [FromForm] Guid documentId, [FromForm] string? sha256, [FromForm] string? language,
            AcceptOperationsLegalTermsUseCase accept, GetOperationsLegalStatusUseCase status, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            AcceptOperationsLegalTermsResponse response = await Unavailable(() => accept.ExecuteAsync(OperationsCurrentUser.Context(http),
                new AcceptOperationsLegalTermsRequest { DocumentId = documentId, Sha256 = sha256 ?? string.Empty, Language = language ?? "en" }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/legal?done=1")
                : await LegalPageAsync(http, status, await CompanyAsync(http, brand), text, response.Error, null);
        }).RequireAuthorization();

        endpoints.MapGet("/staff/legal/documents/{documentId:guid}/{language}", async (Guid documentId, string language, HttpContext http, IOperationsLegalClient legal) =>
        {
            if (!Vixly.Operations.Web.Site.OperationsSiteLanguages.IsSiteLanguage(language))
            {
                return Results.NotFound();
            }

            OperationsLegalFile? file = await Unavailable(() => legal.GetFileAsync(documentId, language, http.RequestAborted));
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return file is null ? Results.NotFound() : Results.File(file.Content, "application/pdf", "terms-" + language + ".pdf");
        }).RequireAuthorization();

        endpoints.MapGet("/staff/limited", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            Page<AccessLimitedPage>(new
            {
                Company = await CompanyAsync(http, brand),
                Reason = OperationsTenantMiddleware.Host(http) is { } host ? AccessBanner.Reason(host) : "read_only"
            })).RequireAuthorization();

        endpoints.MapGet("/account/second-factor-reset", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            Page<SecondFactorResetPage>(new { Company = await CompanyAsync(http, brand) }));

        endpoints.MapPost("/account/second-factor-reset", async (HttpContext http, [FromForm] string? token, [FromForm] string? email, [FromForm] string? password,
            CompleteOperationsSecondFactorResetUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            CompleteOperationsSecondFactorResetResponse response = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new CompleteOperationsSecondFactorResetRequest { Token = token ?? string.Empty, Email = email ?? string.Empty, Password = password ?? string.Empty }, http.RequestAborted));

            if (!response.Succeeded)
            {
                return Page<SecondFactorResetPage>(new { Company = await CompanyAsync(http, brand), Error = text.Describe(response.Error) });
            }

            OperationsCookies.Delete(http.Response, OperationsCookies.Session);
            OperationsCookies.Write(http.Response, OperationsCookies.SignIn, response.ChallengeToken, OperationsSignInChallenge.Lifetime);
            return Results.Redirect("/staff/sign-in/enrol?returnUrl=%2Fstaff");
        }).RequireRateLimiting(OperationsRateLimits.AccountLink);

        return endpoints;
    }

    private static async Task<IResult> LegalPageAsync(HttpContext http, GetOperationsLegalStatusUseCase useCase, string? company, OperationsErrorText text, Vixly.Shared.Kernel.Errors.Error? error, string? info)
    {
        string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        GetOperationsLegalStatusResponse status = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsLegalStatusRequest { Language = language }, http.RequestAborted));
        http.Response.Headers.CacheControl = "no-store";

        return Page<LegalPage>(new
        {
            Company = company,
            Status = status.Status,
            IsOwner = http.User.FindFirst(OperationsClaimTypes.Owner)?.Value == "1",
            Error = text.Describe(error ?? status.Error),
            Info = info
        });
    }

    private static async Task<T> Unavailable<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (OperationsPlatformUnavailableException exception)
        {
            throw new BadHttpRequestException("Platform is not available.", StatusCodes.Status503ServiceUnavailable, exception);
        }
    }

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        return new RazorComponentResult<TComponent>(parameters);
    }

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand)
    {
        GetOperationsSignInBrandResponse response = await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
        return response.DisplayName;
    }
}
