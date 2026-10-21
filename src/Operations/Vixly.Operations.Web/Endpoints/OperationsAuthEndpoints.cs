using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QRCoder;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.SignIn;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsAuthEndpoints.cs
//
// PURPOSE:
// The staff landing page and sign-in steps of a tenant (Slice 1).
//
// RESPONSIBILITIES:
// - GET /staff: the staff sign-in form, or the workspace once signed in (moved from /, Slice 1B)
// - Password, authenticator code, recovery code and enrolment steps
// - Owner invitation, sign-out, language and session keep-alive
// - GET /setup: the owner setup page (sign-in first)
//
// IMPORTANT NOTES:
// - Static server-rendered forms with antiforgery (ADR-0019 form guard)
// - The tenant comes from the host; the user from the session cookie (S-2)
// - Rate limited per address (OperationsRateLimits)
// =========================================================
public static class OperationsAuthEndpoints
{
    public static IEndpointRouteBuilder MapOperationsAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/staff", async (HttpContext http, string? reset, string? device, string? idle, string? ended, GetOperationsSignInBrandUseCase brand, IStringLocalizer<OperationsText> text,
            Vixly.Operations.Application.UseCases.Settings.GetOperationsSetupChecklist.GetOperationsSetupChecklistUseCase checklist,
            Vixly.Operations.Application.UseCases.Supplies.ListOperationsLowSupplies.ListOperationsLowSuppliesUseCase lowSupplies) =>
            OperationsCurrentUser.IsSignedIn(http)
                ? Page<WorkspacePage>(new
                {
                    Company = await CompanyAsync(http, brand),
                    Name = OperationsCurrentUser.DisplayName(http),
                    Checklist = (await checklist.ExecuteAsync(OperationsCurrentUser.Context(http), new Vixly.Operations.Application.UseCases.Settings.GetOperationsSetupChecklist.GetOperationsSetupChecklistRequest(), http.RequestAborted)).Items,
                    LowSupplies = (await lowSupplies.ExecuteAsync(OperationsCurrentUser.Context(http), new Vixly.Operations.Application.UseCases.Supplies.ListOperationsLowSupplies.ListOperationsLowSuppliesRequest
                    {
                        Language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                    }, http.RequestAborted)).Items
                })
                : Page<SignInPage>(new
                {
                    Company = await CompanyAsync(http, brand),
                    ReturnUrl = "/staff",
                    Info = reset is not null ? text["ui.reset.done"].Value : device is not null ? text["ui.devices.ready"].Value
                        : idle is not null ? text["ui.session.signed_out_idle"].Value : ended is not null ? text["ui.session.ended"].Value : null
                }));

        endpoints.MapGet("/setup", async (HttpContext http, string? link, GetOperationsSignInBrandUseCase brand, IStringLocalizer<OperationsText> text) =>
            OperationsCurrentUser.IsSignedIn(http)
                ? Page<SetupHomePage>(new
                {
                    Company = await CompanyAsync(http, brand),
                    Name = OperationsCurrentUser.DisplayName(http),
                    LinkSent = link is not null,
                    Info = link is not null ? text["ui.setup.device_sent"].Value : null
                })
                : Page<SignInPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = "/setup", Setup = true }));

        endpoints.MapPost("/staff/sign-in", async (HttpContext http, [FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl,
            BeginOperationsSignInUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            BeginOperationsSignInResponse response = await useCase.ExecuteAsync(
                OperationsCurrentUser.Context(http), new BeginOperationsSignInRequest { Email = email, Password = password }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<SignInPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl), Email = email, Error = text.Describe(response.Error) });
            }

            if (response.SessionToken.Length > 0)
            {
                StartSession(http, response.SessionToken);
                return Results.Redirect(Back(http, returnUrl));
            }

            OperationsCookies.Write(http.Response, OperationsCookies.SignIn, response.ChallengeToken, OperationsSignInChallenge.Lifetime);
            return Results.Redirect((response.EnrolmentRequired ? "/staff/sign-in/enrol" : "/staff/sign-in/verify") + "?returnUrl=" + Uri.EscapeDataString(Back(http, returnUrl)));
        }).RequireRateLimiting(OperationsRateLimits.SignIn);

        endpoints.MapGet("/staff/sign-in/verify", async (HttpContext http, string? returnUrl, GetOperationsSignInBrandUseCase brand) =>
            HasChallenge(http) ? Page<VerifyPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl) }) : Results.Redirect("/staff"));

        endpoints.MapPost("/staff/sign-in/verify", async (HttpContext http, [FromForm] string code, [FromForm] string? returnUrl,
            CompleteOperationsSignInWithAuthenticatorCodeUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            CompleteOperationsSignInWithAuthenticatorCodeResponse response = await useCase.ExecuteAsync(
                OperationsCurrentUser.Context(http), new CompleteOperationsSignInWithAuthenticatorCodeRequest { ChallengeToken = Challenge(http), Code = code }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<VerifyPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Results.Redirect(Back(http, returnUrl));
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        endpoints.MapGet("/staff/sign-in/recovery", async (HttpContext http, string? returnUrl, GetOperationsSignInBrandUseCase brand) =>
            HasChallenge(http) ? Page<RecoveryPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl) }) : Results.Redirect("/staff"));

        endpoints.MapPost("/staff/sign-in/recovery", async (HttpContext http, [FromForm] string code, [FromForm] string? returnUrl,
            CompleteOperationsSignInWithRecoveryCodeUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            CompleteOperationsSignInWithRecoveryCodeResponse response = await useCase.ExecuteAsync(
                OperationsCurrentUser.Context(http), new CompleteOperationsSignInWithRecoveryCodeRequest { ChallengeToken = Challenge(http), Code = code }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<RecoveryPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Results.Redirect(Back(http, returnUrl));
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        endpoints.MapGet("/staff/sign-in/enrol", async (HttpContext http, string? returnUrl, BeginOperationsSignInEnrolmentUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            if (!HasChallenge(http))
            {
                return Results.Redirect("/staff");
            }

            BeginOperationsSignInEnrolmentResponse response = await useCase.ExecuteAsync(
                OperationsCurrentUser.Context(http), new BeginOperationsSignInEnrolmentRequest { ChallengeToken = Challenge(http) }, http.RequestAborted);

            return response.Succeeded
                ? Page<EnrolPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl), response.ManualKey, QrDataUri = QrDataUri(response.OtpAuthUri) })
                : Page<SignInPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(http, returnUrl), Error = text.Describe(response.Error) });
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        endpoints.MapPost("/staff/sign-in/enrol", async (HttpContext http, [FromForm] string code, [FromForm] string? returnUrl,
            CompleteOperationsSignInEnrolmentUseCase useCase, BeginOperationsSignInEnrolmentUseCase begin, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            OperationsAuthorizationContext context = OperationsCurrentUser.Context(http);
            CompleteOperationsSignInEnrolmentResponse response = await useCase.ExecuteAsync(
                context, new CompleteOperationsSignInEnrolmentRequest { ChallengeToken = Challenge(http), Code = code }, http.RequestAborted);
            string? company = await CompanyAsync(http, brand);

            if (!response.Succeeded)
            {
                BeginOperationsSignInEnrolmentResponse again = await begin.ExecuteAsync(context, new BeginOperationsSignInEnrolmentRequest { ChallengeToken = Challenge(http) }, http.RequestAborted);

                return again.Succeeded
                    ? Page<EnrolPage>(new { Company = company, ReturnUrl = Back(http, returnUrl), again.ManualKey, QrDataUri = QrDataUri(again.OtpAuthUri), Error = text.Describe(response.Error) })
                    : Page<SignInPage>(new { Company = company, ReturnUrl = Back(http, returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Page<RecoveryCodesPage>(new { Company = company, ReturnUrl = Back(http, returnUrl), Codes = response.RecoveryCodes });
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        endpoints.MapGet("/invitation", async (HttpContext http, string? token, GetOperationsSignInBrandUseCase brand) =>
            Page<InvitationPage>(new { Company = await CompanyAsync(http, brand), Token = token ?? string.Empty }));

        endpoints.MapPost("/invitation", async (HttpContext http, [FromForm] string token, [FromForm] string password,
            AcceptOperationsInvitationUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            AcceptOperationsInvitationResponse response = await useCase.ExecuteAsync(
                OperationsCurrentUser.Context(http), new AcceptOperationsInvitationRequest { Token = token, Password = password }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<InvitationPage>(new { Company = await CompanyAsync(http, brand), Token = token, Error = text.Describe(response.Error) });
            }

            if (response.SessionToken.Length > 0)
            {
                StartSession(http, response.SessionToken);
                return Results.Redirect(Back(http, null));
            }

            OperationsCookies.Write(http.Response, OperationsCookies.SignIn, response.ChallengeToken, OperationsSignInChallenge.Lifetime);
            return Results.Redirect("/staff/sign-in/enrol?returnUrl=" + Uri.EscapeDataString(Back(http, null)));
        }).RequireRateLimiting(OperationsRateLimits.AccountLink);

        // The idle timer signs out through this form too (reason=idle), so the session really ends and the sign-in page says why (owner, 2026-10-20).
        endpoints.MapPost("/staff/sign-out", async (HttpContext http, [FromForm] bool? everywhere, [FromForm] string? reason, SignOutOperationsUseCase useCase) =>
        {
            await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new SignOutOperationsRequest { EverywhereElseToo = everywhere == true }, http.RequestAborted);
            OperationsCookies.Delete(http.Response, OperationsCookies.Session);
            return Results.Redirect(reason == "idle" ? "/staff?idle=1" : "/staff");
        }).RequireAuthorization();

        endpoints.MapGet("/session/keepalive", () => Results.NoContent()).RequireAuthorization();

        endpoints.MapGet("/culture/{code}", (HttpContext http, string code, string? returnUrl) =>
        {
            if (LanguageCode.TryCreate(code, out LanguageCode? language))
            {
                OperationsCookies.Write(http.Response, OperationsCookies.Culture, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language!.Value)), TimeSpan.FromDays(365));
            }

            return Results.Redirect(LocalUrl(returnUrl) ?? "/");
        });

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
        return response.Succeeded ? response.DisplayName : null;
    }

    private static bool HasChallenge(HttpContext http) => http.Request.Cookies.ContainsKey(OperationsCookies.SignIn);

    private static string Challenge(HttpContext http) => http.Request.Cookies.TryGetValue(OperationsCookies.SignIn, out string? value) ? value ?? string.Empty : string.Empty;

    private static void StartSession(HttpContext http, string sessionToken)
    {
        OperationsCookies.Delete(http.Response, OperationsCookies.SignIn);
        OperationsCookies.Write(http.Response, OperationsCookies.Session, sessionToken, null);
    }

    // Where to go after sign-in: the asked local page, or the tenant start page (setup while being set up).
    private static string Back(HttpContext http, string? returnUrl)
    {
        return LocalUrl(returnUrl) ?? (OperationsCurrentUser.Context(http).IsSettingUp ? "/setup" : "/staff");
    }

    private static string? LocalUrl(string? url)
    {
        return !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\')) ? url : null;
    }

    private static string QrDataUri(string otpAuthUri)
    {
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
        string svg = new SvgQRCode(data).GetGraphic(5);

        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }
}
