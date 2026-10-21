using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.SignIn.RequestOperationsPasswordReset;
using Vixly.Operations.Application.UseCases.SignIn.ResetOperationsPassword;
using Vixly.Operations.Web.Components.SignIn;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsPasswordEndpoints.cs
//
// PURPOSE:
// "Forgot your password?" and the reset link of a staff account (Slice 1C).
//
// RESPONSIBILITIES:
// - GET and POST /staff/forgot-password: email a reset link
// - GET and POST /staff/reset-password: set the new password
//
// IMPORTANT NOTES:
// - The same answer whether or not the address has an account
// - 5 requests per hour per address; reset like the other account links (10 per hour)
// - The token is in the link; the page posts it once and sends no referrer
// =========================================================
public static class OperationsPasswordEndpoints
{
    public static IEndpointRouteBuilder MapOperationsPasswordEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/staff/forgot-password", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            Page<ForgotPasswordPage>(new { Company = await CompanyAsync(http, brand) }));

        endpoints.MapPost("/staff/forgot-password", async (HttpContext http, [FromForm] string? email, RequestOperationsPasswordResetUseCase useCase, GetOperationsSignInBrandUseCase brand) =>
        {
            await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RequestOperationsPasswordResetRequest { Email = email ?? string.Empty, PortalSlug = OperationsTenantMiddleware.Host(http)?.CanonicalSlug ?? string.Empty }, http.RequestAborted);
            return Page<ForgotPasswordPage>(new { Company = await CompanyAsync(http, brand), Sent = true });
        }).RequireRateLimiting(OperationsRateLimits.PasswordReset);

        // The sign-in shell sends no referrer (meta tag), so the token never leaves in a Referer header.
        endpoints.MapGet("/staff/reset-password", async (HttpContext http, string? token, GetOperationsSignInBrandUseCase brand) =>
            Page<ResetPasswordPage>(new { Company = await CompanyAsync(http, brand), Token = token ?? string.Empty }));

        endpoints.MapPost("/staff/reset-password", async (HttpContext http, [FromForm] string? token, [FromForm] string? password,
            ResetOperationsPasswordUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            ResetOperationsPasswordResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ResetOperationsPasswordRequest { Token = token ?? string.Empty, Password = password ?? string.Empty }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<ResetPasswordPage>(new { Company = await CompanyAsync(http, brand), Token = token ?? string.Empty, Error = text.Describe(response.Error) });
            }

            OperationsCookies.Delete(http.Response, OperationsCookies.Session);
            return Results.Redirect("/staff?reset=done");
        }).RequireRateLimiting(OperationsRateLimits.AccountLink);

        return endpoints;
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
