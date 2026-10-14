using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.SignIn.AcceptInvitation;
using Vixly.Platform.Application.UseCases.SignIn.BeginPasskeySignIn;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignInEnrolment;
using Vixly.Platform.Application.UseCases.SignIn.CompletePasskeySignIn;
using Vixly.Platform.Application.UseCases.SignIn.CompleteSignInEnrolment;
using Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithAuthenticatorCode;
using Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithRecoveryCode;
using Vixly.Platform.Application.UseCases.SignIn.RequestPasswordReset;
using Vixly.Platform.Application.UseCases.SignIn.ResetPassword;
using Vixly.Platform.Application.UseCases.SignIn.SignOut;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Web.Components.SignIn;
using Vixly.Platform.Web.Hosting;
using Vixly.Platform.Web.Security;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Web.Endpoints;

// =========================================================
// FILE: PlatformAuthEndpoints.cs
//
// PURPOSE:
// The sign-in, invitation, password-reset and sign-out
// endpoints of the console (ADR-0006).
//
// RESPONSIBILITIES:
// - Render each step as a static page (no JavaScript) and
//   handle its form post with antiforgery
// - Call the matching use case; set or clear the challenge
//   and session cookies
// - Apply the named rate-limit policy to every anonymous
//   post (S-6)
//
// IMPORTANT NOTES:
// - These endpoints are the presenters of the sign-in pages:
//   pages receive ready text and values only (ADR-0001)
// - Return URLs must be local; anything else goes to /
// - Anonymous endpoint register: /sign-in, /sign-in/verify,
//   /sign-in/recovery, /sign-in/enrol, /sign-in/passkey,
//   /sign-in/passkey/options, /invitation, /forgot-password,
//   /reset-password (plane document)
// - The passkey button is the only script on these pages, and
//   the password form works without it
// =========================================================
public static class PlatformAuthEndpoints
{
    public static IEndpointRouteBuilder MapPlatformAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        PlatformAnonymousAuthorizationContext anonymous = PlatformAnonymousAuthorizationContext.Instance;

        endpoints.MapGet("/sign-in", (string? returnUrl, string? done, PlatformErrorText text) =>
            Page<SignInPage>(new
            {
                ReturnUrl = LocalUrl(returnUrl),
                Info = done == "reset" ? text.Text("ui.reset.done") : null
            }));

        endpoints.MapPost("/sign-in", async (
            HttpContext http,
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string? returnUrl,
            BeginSignInUseCase useCase,
            PlatformErrorText text) =>
        {
            BeginSignInResponse response = await useCase.ExecuteAsync(
                anonymous,
                new BeginSignInRequest { Email = email, Password = password },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<SignInPage>(new { ReturnUrl = LocalUrl(returnUrl), Email = email, Error = text.Describe(response.Error) });
            }

            PlatformCookies.Write(http.Response, PlatformCookies.SignInChallenge, response.ChallengeToken, PlatformSignInChallenge.Lifetime);
            string next = response.EnrolmentRequired ? "/sign-in/enrol" : "/sign-in/verify";

            return Results.Redirect(next + "?returnUrl=" + Uri.EscapeDataString(LocalUrl(returnUrl)));
        }).RequireRateLimiting(PlatformRateLimits.SignIn);

        endpoints.MapPost("/sign-in/passkey/options", async (
            HttpContext http,
            [FromForm] string? returnUrl,
            BeginPasskeySignInUseCase useCase) =>
        {
            BeginPasskeySignInResponse response = await useCase.ExecuteAsync(anonymous, new BeginPasskeySignInRequest(), http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.StatusCode(StatusCodes.Status409Conflict);
            }

            http.Response.Headers.CacheControl = "no-store";
            PlatformCookies.Write(http.Response, PlatformCookies.PasskeyCeremony, response.CeremonyToken, PlatformPasskeyCeremony.Lifetime);

            return Results.Content(response.OptionsJson, "application/json");
        }).RequireRateLimiting(PlatformRateLimits.Passkey);

        endpoints.MapPost("/sign-in/passkey", async (
            HttpContext http,
            [FromForm] string assertion,
            [FromForm] string? returnUrl,
            CompletePasskeySignInUseCase useCase,
            PlatformErrorText text) =>
        {
            string ceremony = http.Request.Cookies.TryGetValue(PlatformCookies.PasskeyCeremony, out string? value) ? value ?? string.Empty : string.Empty;
            PlatformCookies.Delete(http.Response, PlatformCookies.PasskeyCeremony);

            CompletePasskeySignInResponse response = await useCase.ExecuteAsync(
                anonymous,
                new CompletePasskeySignInRequest { CeremonyToken = ceremony, AssertionJson = assertion },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<SignInPage>(new { ReturnUrl = LocalUrl(returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Results.Redirect(LocalUrl(returnUrl));
        }).RequireRateLimiting(PlatformRateLimits.Passkey);

        endpoints.MapGet("/sign-in/verify", (HttpContext http, string? returnUrl) =>
            HasChallenge(http)
                ? Page<VerifyPage>(new { ReturnUrl = LocalUrl(returnUrl) })
                : Results.Redirect("/sign-in"));

        endpoints.MapPost("/sign-in/verify", async (
            HttpContext http,
            [FromForm] string code,
            [FromForm] string? returnUrl,
            CompleteSignInWithAuthenticatorCodeUseCase useCase,
            PlatformErrorText text) =>
        {
            CompleteSignInWithAuthenticatorCodeResponse response = await useCase.ExecuteAsync(
                anonymous,
                new CompleteSignInWithAuthenticatorCodeRequest { ChallengeToken = Challenge(http), Code = code },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<VerifyPage>(new { ReturnUrl = LocalUrl(returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Results.Redirect(LocalUrl(returnUrl));
        }).RequireRateLimiting(PlatformRateLimits.SecondFactor);

        endpoints.MapGet("/sign-in/recovery", (HttpContext http, string? returnUrl) =>
            HasChallenge(http)
                ? Page<RecoveryPage>(new { ReturnUrl = LocalUrl(returnUrl) })
                : Results.Redirect("/sign-in"));

        endpoints.MapPost("/sign-in/recovery", async (
            HttpContext http,
            [FromForm] string code,
            [FromForm] string? returnUrl,
            CompleteSignInWithRecoveryCodeUseCase useCase,
            PlatformErrorText text) =>
        {
            CompleteSignInWithRecoveryCodeResponse response = await useCase.ExecuteAsync(
                anonymous,
                new CompleteSignInWithRecoveryCodeRequest { ChallengeToken = Challenge(http), Code = code },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<RecoveryPage>(new { ReturnUrl = LocalUrl(returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Results.Redirect(LocalUrl(returnUrl));
        }).RequireRateLimiting(PlatformRateLimits.SecondFactor);

        endpoints.MapGet("/sign-in/enrol", async (
            HttpContext http,
            string? returnUrl,
            BeginSignInEnrolmentUseCase useCase,
            PlatformErrorText text) =>
        {
            if (!HasChallenge(http))
            {
                return Results.Redirect("/sign-in");
            }

            BeginSignInEnrolmentResponse response = await useCase.ExecuteAsync(
                anonymous,
                new BeginSignInEnrolmentRequest { ChallengeToken = Challenge(http) },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<SignInPage>(new { ReturnUrl = LocalUrl(returnUrl), Error = text.Describe(response.Error) });
            }

            return Page<EnrolPage>(new
            {
                ReturnUrl = LocalUrl(returnUrl),
                ManualKey = response.ManualKey,
                QrDataUri = QrDataUri(response.OtpAuthUri)
            });
        }).RequireRateLimiting(PlatformRateLimits.SecondFactor);

        endpoints.MapPost("/sign-in/enrol", async (
            HttpContext http,
            [FromForm] string code,
            [FromForm] string? returnUrl,
            CompleteSignInEnrolmentUseCase useCase,
            BeginSignInEnrolmentUseCase begin,
            PlatformErrorText text) =>
        {
            CompleteSignInEnrolmentResponse response = await useCase.ExecuteAsync(
                anonymous,
                new CompleteSignInEnrolmentRequest { ChallengeToken = Challenge(http), Code = code },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                BeginSignInEnrolmentResponse again = await begin.ExecuteAsync(
                    anonymous,
                    new BeginSignInEnrolmentRequest { ChallengeToken = Challenge(http) },
                    http.RequestAborted);

                return again.Succeeded
                    ? Page<EnrolPage>(new
                    {
                        ReturnUrl = LocalUrl(returnUrl),
                        ManualKey = again.ManualKey,
                        QrDataUri = QrDataUri(again.OtpAuthUri),
                        Error = text.Describe(response.Error)
                    })
                    : Page<SignInPage>(new { ReturnUrl = LocalUrl(returnUrl), Error = text.Describe(response.Error) });
            }

            StartSession(http, response.SessionToken);
            return Page<RecoveryCodesPage>(new { ReturnUrl = LocalUrl(returnUrl), Codes = response.RecoveryCodes });
        }).RequireRateLimiting(PlatformRateLimits.SecondFactor);

        endpoints.MapGet("/invitation", (string? token) =>
            Page<InvitationPage>(new { Token = token ?? string.Empty }));

        endpoints.MapPost("/invitation", async (
            HttpContext http,
            [FromForm] string token,
            [FromForm] string password,
            AcceptInvitationUseCase useCase,
            PlatformErrorText text) =>
        {
            AcceptInvitationResponse response = await useCase.ExecuteAsync(
                anonymous,
                new AcceptInvitationRequest { Token = token, Password = password },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<InvitationPage>(new { Token = token, Error = text.Describe(response.Error) });
            }

            PlatformCookies.Write(http.Response, PlatformCookies.SignInChallenge, response.ChallengeToken, PlatformSignInChallenge.Lifetime);
            return Results.Redirect("/sign-in/enrol");
        }).RequireRateLimiting(PlatformRateLimits.AccountLink);

        endpoints.MapGet("/forgot-password", () => Page<ForgotPasswordPage>(new { }));

        endpoints.MapPost("/forgot-password", async (
            HttpContext http,
            [FromForm] string email,
            RequestPasswordResetUseCase useCase) =>
        {
            await useCase.ExecuteAsync(anonymous, new RequestPasswordResetRequest { Email = email }, http.RequestAborted);
            return Page<ForgotPasswordPage>(new { Sent = true });
        }).RequireRateLimiting(PlatformRateLimits.PasswordReset);

        endpoints.MapGet("/reset-password", (string? token) =>
            Page<ResetPasswordPage>(new { Token = token ?? string.Empty }));

        endpoints.MapPost("/reset-password", async (
            HttpContext http,
            [FromForm] string token,
            [FromForm] string password,
            ResetPasswordUseCase useCase,
            PlatformErrorText text) =>
        {
            ResetPasswordResponse response = await useCase.ExecuteAsync(
                anonymous,
                new ResetPasswordRequest { Token = token, NewPassword = password },
                http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/sign-in?done=reset")
                : Page<ResetPasswordPage>(new { Token = token, Error = text.Describe(response.Error) });
        }).RequireRateLimiting(PlatformRateLimits.AccountLink);

        endpoints.MapPost("/sign-out", async (
            HttpContext http,
            [FromForm] bool? everywhere,
            SignOutUseCase useCase) =>
        {
            await useCase.ExecuteAsync(
                PlatformCurrentUser.FromPrincipal(http.User),
                new SignOutRequest { EverywhereElseToo = everywhere == true },
                http.RequestAborted);
            PlatformCookies.Delete(http.Response, PlatformCookies.Session);

            return Results.Redirect("/sign-in");
        }).RequireAuthorization();

        endpoints.MapGet("/session/keepalive", () => Results.NoContent()).RequireAuthorization();

        endpoints.MapGet("/culture/{code}", (HttpContext http, string code, string? returnUrl) =>
        {
            if (LanguageCode.TryCreate(code, out LanguageCode? language))
            {
                PlatformCookies.Write(
                    http.Response,
                    PlatformCookies.Culture,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language!.Value)),
                    TimeSpan.FromDays(365));
            }

            return Results.Redirect(LocalUrl(returnUrl));
        });

        return endpoints;
    }

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        return new RazorComponentResult<TComponent>(parameters);
    }

    private static bool HasChallenge(HttpContext http)
    {
        return http.Request.Cookies.ContainsKey(PlatformCookies.SignInChallenge);
    }

    private static string Challenge(HttpContext http)
    {
        return http.Request.Cookies.TryGetValue(PlatformCookies.SignInChallenge, out string? value) ? value ?? string.Empty : string.Empty;
    }

    private static void StartSession(HttpContext http, string sessionToken)
    {
        PlatformCookies.Delete(http.Response, PlatformCookies.SignInChallenge);
        PlatformCookies.Write(http.Response, PlatformCookies.Session, sessionToken, null);
    }

    private static string LocalUrl(string? url)
    {
        return !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            ? url
            : "/";
    }

    private static string QrDataUri(string otpAuthUri)
    {
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
        string svg = new SvgQRCode(data).GetGraphic(5);

        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }
}
