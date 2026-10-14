using Vixly.Contracts.Platform.Registration;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Registration.CheckPortalSlugAvailability;
using Vixly.Platform.Application.UseCases.Registration.CorrectRegistrationEmail;
using Vixly.Platform.Application.UseCases.Registration.GetMyRegistrationStatus;
using Vixly.Platform.Application.UseCases.Registration.ResendMyRegistrationVerification;
using Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformRegistrationEndpoints.cs
//
// PURPOSE:
// The anonymous registration endpoints of Platform.Api (S-6).
//
// RESPONSIBILITIES:
// - Map each endpoint to its use case and rate-limit policy
// - Read the access token from the Authorization header
//
// IMPORTANT NOTES:
// - Every endpoint here is in the anonymous-endpoint register of
//   the Slice 2 design note, section 6
// - Responses are never cached
// =========================================================
public static class PlatformRegistrationEndpoints
{
    private static readonly PlatformAnonymousAuthorizationContext Anonymous = PlatformAnonymousAuthorizationContext.Instance;

    public static IEndpointRouteBuilder MapPlatformRegistrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/registration").AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });

        group.MapGet("/slugs/{slug}", async (
            string slug,
            CheckPortalSlugAvailabilityUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            CheckPortalSlugAvailabilityResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new CheckPortalSlugAvailabilityRequest { Slug = slug },
                cancellationToken);

            return Results.Ok(response.Succeeded
                ? new PlatformSlugAvailability(true, response.Slug, null)
                : new PlatformSlugAvailability(false, null, errors.Describe(response.Error!)));
        }).RequireRateLimiting(PlatformApiRateLimits.SlugCheck).RequireCors(PlatformApiHost.CorsPolicy);

        group.MapPost("/", async (
            StartPlatformRegistrationBody body,
            StartPlatformTenantRegistrationUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            StartPlatformTenantRegistrationResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new StartPlatformTenantRegistrationRequest
                {
                    Company = new PlatformCompanyInput
                    {
                        LegalName = body.LegalName,
                        TradingName = body.TradingName,
                        Email = body.CompanyEmail,
                        Phone = body.CompanyPhone,
                        AddressLine1 = body.AddressLine1,
                        AddressLine2 = body.AddressLine2,
                        City = body.City,
                        StateProvince = body.StateProvince,
                        PostalCode = body.PostalCode,
                        CountryCode = body.CountryCode
                    },
                    OwnerName = body.OwnerName,
                    OwnerEmail = body.OwnerEmail,
                    OwnerPhone = body.OwnerPhone,
                    Language = body.Language,
                    TimeZone = body.TimeZone,
                    Slug = body.Slug,
                    AcceptedTerms = body.AcceptedTerms,
                    TermsVersion = body.TermsVersion,
                    PrivacyVersion = body.PrivacyVersion,
                    BotToken = body.BotToken
                },
                cancellationToken);

            return response.Succeeded
                ? Results.Ok(new PlatformRegistrationAccess(response.AccessToken, response.AccessExpiresUtc, null))
                : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Start);

        group.MapPost("/verify", async (
            VerifyPlatformRegistrationBody body,
            VerifyRegistrationEmailUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            VerifyRegistrationEmailResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new VerifyRegistrationEmailRequest { Token = body.Token },
                cancellationToken);

            return response.Succeeded
                ? Results.Ok(new PlatformRegistrationAccess(response.AccessToken, DateTimeOffset.MinValue, response.TenantCode))
                : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Verify);

        group.MapPost("/email", async (
            HttpContext http,
            CorrectPlatformRegistrationEmailBody body,
            CorrectRegistrationEmailUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            CorrectRegistrationEmailResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new CorrectRegistrationEmailRequest { AccessToken = Bearer(http), Email = body.Email },
                cancellationToken);

            return response.Succeeded ? Results.NoContent() : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Change);

        group.MapPost("/resend", async (
            HttpContext http,
            ResendMyRegistrationVerificationUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            ResendMyRegistrationVerificationResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new ResendMyRegistrationVerificationRequest { AccessToken = Bearer(http) },
                cancellationToken);

            return response.Succeeded ? Results.NoContent() : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Change);

        group.MapGet("/status", async (
            HttpContext http,
            GetMyRegistrationStatusUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            GetMyRegistrationStatusResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new GetMyRegistrationStatusRequest { AccessToken = Bearer(http) },
                cancellationToken);

            return response.Succeeded
                ? Results.Ok(new PlatformRegistrationStatus(StepCode(response.Status), response.MaskedEmail, response.Slug, response.VerificationDeadlineUtc))
                : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Status);

        return endpoints;
    }

    public static string StepCode(PlatformTenantRegistrationStatus status)
    {
        return status switch
        {
            PlatformTenantRegistrationStatus.PendingEmailVerification => "pending_email_verification",
            PlatformTenantRegistrationStatus.PendingPlanSelection => "pending_plan_selection",
            PlatformTenantRegistrationStatus.PendingPayment => "pending_payment",
            PlatformTenantRegistrationStatus.PendingAccountSetup => "pending_account_setup",
            PlatformTenantRegistrationStatus.PendingFirstDeviceRegistration => "pending_first_device",
            PlatformTenantRegistrationStatus.Completed => "completed",
            PlatformTenantRegistrationStatus.Cancelled => "cancelled",
            _ => "expired"
        };
    }

    private static string Bearer(HttpContext http)
    {
        string header = http.Request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : string.Empty;
    }
}
