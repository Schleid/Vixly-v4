using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsQuickSignIn;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithPassword;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.SignIn.ListOperationsDevicePeople;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Web.Components.SignIn;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsQuickSignInEndpoints.cs
//
// PURPOSE:
// Badge and PIN sign-in on a licensed device (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - /staff/sign-in/badge: the badge screen and its form
// - /staff/sign-in/pin: the people of the device, then the PIN
// - /staff/sign-in/password: the daily check for a person without an authenticator
//
// IMPORTANT NOTES:
// - The device cookie names the device; Platform checks it on every attempt
// - The daily check with an authenticator goes to the usual /staff/sign-in/verify page
// - Rate-limited as the email sign-in; forms with antiforgery
// =========================================================
public static class OperationsQuickSignInEndpoints
{
    public static IEndpointRouteBuilder MapOperationsQuickSignInEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/staff/sign-in/badge", async (HttpContext http, string? returnUrl, GetOperationsSignInBrandUseCase brand) =>
            OperationsCurrentUser.IsSignedIn(http)
                ? Results.Redirect("/staff")
                : Page<QuickSignInPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(returnUrl) }));

        endpoints.MapPost("/staff/sign-in/badge", async (HttpContext http, [FromForm] string? badge, [FromForm] string? returnUrl,
            BeginOperationsQuickSignInUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            BeginOperationsQuickSignInResponse response = await BeginAsync(http, useCase, new BeginOperationsQuickSignInRequest { Badge = badge ?? string.Empty });

            return response.Succeeded
                ? Continue(http, response, returnUrl)
                : Page<QuickSignInPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(returnUrl), Error = text.Describe(response.Error) });
        }).RequireRateLimiting(OperationsRateLimits.SignIn);

        endpoints.MapGet("/staff/sign-in/pin", async (HttpContext http, Guid? person, string? returnUrl, ListOperationsDevicePeopleUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText text) =>
        {
            if (OperationsCurrentUser.IsSignedIn(http))
            {
                return Results.Redirect("/staff");
            }

            ListOperationsDevicePeopleResponse people = await PeopleAsync(http, list);
            return Page<PinSignInPage>(new
            {
                Company = await CompanyAsync(http, brand),
                ReturnUrl = Back(returnUrl),
                People = people.People,
                Chosen = people.People.FirstOrDefault(item => item.UserId == person),
                Error = people.Succeeded ? null : text.Describe(people.Error)
            });
        });

        endpoints.MapPost("/staff/sign-in/pin", async (HttpContext http, [FromForm] Guid userId, [FromForm] string? pin, [FromForm] string? returnUrl,
            BeginOperationsQuickSignInUseCase useCase, ListOperationsDevicePeopleUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            BeginOperationsQuickSignInResponse response = await BeginAsync(http, useCase, new BeginOperationsQuickSignInRequest { UserId = userId, Pin = pin ?? string.Empty });

            if (response.Succeeded)
            {
                return Continue(http, response, returnUrl);
            }

            ListOperationsDevicePeopleResponse people = await PeopleAsync(http, list);
            return Page<PinSignInPage>(new
            {
                Company = await CompanyAsync(http, brand),
                ReturnUrl = Back(returnUrl),
                People = people.People,
                Chosen = people.People.FirstOrDefault(item => item.UserId == userId),
                Error = text.Describe(response.Error)
            });
        }).RequireRateLimiting(OperationsRateLimits.SignIn);

        endpoints.MapGet("/staff/sign-in/password", async (HttpContext http, string? returnUrl, GetOperationsSignInBrandUseCase brand) =>
            http.Request.Cookies.ContainsKey(OperationsCookies.SignIn)
                ? Page<PasswordCheckPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(returnUrl) })
                : Results.Redirect("/staff"));

        endpoints.MapPost("/staff/sign-in/password", async (HttpContext http, [FromForm] string? password, [FromForm] string? returnUrl,
            CompleteOperationsSignInWithPasswordUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            CompleteOperationsSignInWithPasswordResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new CompleteOperationsSignInWithPasswordRequest
            {
                ChallengeToken = http.Request.Cookies.TryGetValue(OperationsCookies.SignIn, out string? challenge) ? challenge ?? string.Empty : string.Empty,
                Password = password ?? string.Empty
            }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Page<PasswordCheckPage>(new { Company = await CompanyAsync(http, brand), ReturnUrl = Back(returnUrl), Error = text.Describe(response.Error) });
            }

            Started(http, response.SessionToken);
            return Results.Redirect(Back(returnUrl));
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        return endpoints;
    }

    // Whether /staff opens at the badge screen: a licensed device that used a badge or PIN before.
    public static bool StartsWithBadge(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.Request.Cookies.ContainsKey(OperationsCookies.Quick) && OperationsDeviceCookie.Read(http.Request) is not null;
    }

    private static Task<BeginOperationsQuickSignInResponse> BeginAsync(HttpContext http, BeginOperationsQuickSignInUseCase useCase, BeginOperationsQuickSignInRequest request)
    {
        (Guid DeviceId, string Secret)? device = OperationsDeviceCookie.Read(http.Request);
        return useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new BeginOperationsQuickSignInRequest
        {
            Badge = request.Badge,
            UserId = request.UserId,
            Pin = request.Pin,
            DeviceId = device?.DeviceId,
            DeviceSecret = device?.Secret,
            PortalSlug = Vixly.Operations.Web.Tenancy.OperationsTenantMiddleware.Host(http)?.CanonicalSlug
        }, http.RequestAborted);
    }

    private static async Task<ListOperationsDevicePeopleResponse> PeopleAsync(HttpContext http, ListOperationsDevicePeopleUseCase list)
    {
        (Guid DeviceId, string Secret)? device = OperationsDeviceCookie.Read(http.Request);
        return await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsDevicePeopleRequest { DeviceId = device?.DeviceId, DeviceSecret = device?.Secret },
            http.RequestAborted);
    }

    // Signed in at once, or on to the day's check (the authenticator code, or the password).
    private static IResult Continue(HttpContext http, BeginOperationsQuickSignInResponse response, string? returnUrl)
    {
        OperationsCookies.Write(http.Response, OperationsCookies.Quick, "1", TimeSpan.FromDays(400));

        if (response.SessionToken.Length > 0)
        {
            Started(http, response.SessionToken);
            return Results.Redirect(Back(returnUrl));
        }

        OperationsCookies.Write(http.Response, OperationsCookies.SignIn, response.ChallengeToken, OperationsSignInChallenge.Lifetime);
        return Results.Redirect((response.PasswordCheck ? "/staff/sign-in/password" : "/staff/sign-in/verify") + "?returnUrl=" + Uri.EscapeDataString(Back(returnUrl)));
    }

    private static void Started(HttpContext http, string sessionToken)
    {
        OperationsCookies.Delete(http.Response, OperationsCookies.SignIn);
        OperationsCookies.Write(http.Response, OperationsCookies.Session, sessionToken, null);
    }

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent => new RazorComponentResult<TComponent>(parameters);

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand)
    {
        GetOperationsSignInBrandResponse response = await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
        return response.Succeeded ? response.DisplayName : null;
    }

    private static string Back(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\')) ? url : "/staff";
}
