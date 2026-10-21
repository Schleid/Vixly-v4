using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Internal;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;
using Vixly.Shared.Localization;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerPortalEndpoints.cs
//
// PURPOSE:
// The customer portal pages (Slice 2, design section 4).
//
// RESPONSIBILITIES:
// - Sign-up with the bot check, email confirmation, sign-in and out, forgot and reset password
// - Home with the mailbox addresses; profile and password
//
// IMPORTANT NOTES:
// - Every page is in the tenant brand; nothing is cached (no-store)
// - Operations checks the session on every call (D-C4); a refused session clears the cookie
// - Links carry the token in the query; GET only shows a button, so link scanners never use a link
// =========================================================
public static class CustomerPortalEndpoints
{
    public const string BotField = "cf-turnstile-response";

    public static IEndpointRouteBuilder MapCustomerPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder portal = endpoints.MapGroup(string.Empty);
        portal.WithMetadata(new RequireAntiforgeryTokenAttribute());
        portal.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";

            // A token that failed the antiforgery check stops here, even on a form with no other field (ADR-0019).
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        portal.MapGet("/", async (HttpContext http, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Tenant(http) is not { } tenant)
            {
                return Results.NotFound();
            }

            CustomerPortalBrandDto? brand = await api.GetBrandAsync(tenant, http.RequestAborted);

            if (CustomerSessionCookie.Read(http.Request) is { } session)
            {
                CustomerCall<CustomerProfileDto> me = await api.GetMeAsync(tenant, session, http, http.RequestAborted);

                if (me.Value is { } customer)
                {
                    return Page<HomePage>(new { Brand = brand, Customer = customer, Info = done is null ? null : text["ui.done." + (done == "welcome" ? "welcome" : "saved")].Value });
                }

                CustomerSessionCookie.Delete(http.Response);
            }

            return Page<SignInPage>(new { Brand = brand });
        });

        portal.MapGet("/sign-in", async (HttpContext http, CustomerOperationsApi api) =>
            Tenant(http) is { } tenant ? Page<SignInPage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted) }) : Results.NotFound());

        portal.MapPost("/sign-in", async (HttpContext http, [FromForm] string? email, [FromForm] string? password, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");
            CustomerCall<CustomerSessionDto> call = await api.SignInAsync(tenant, new CustomerSignInBody(email ?? string.Empty, password ?? string.Empty), http, http.RequestAborted);

            if (call.Value is { } session)
            {
                await StartSessionAsync(http, api, tenant, session.SessionToken);
                return Results.Redirect("/customer/");
            }

            return Page<SignInPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Email = email,
                ShowResend = call.ErrorCode == "ops.customers.not_verified",
                Error = Describe(text, call)
            });
        }).RequireRateLimiting(CustomerRateLimits.SignIn);

        portal.MapPost("/sign-out", async (HttpContext http, CustomerOperationsApi api) =>
        {
            if (Tenant(http) is { } tenant && CustomerSessionCookie.Read(http.Request) is { } session)
            {
                await api.SignOutAsync(tenant, session, http, http.RequestAborted);
            }

            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        });

        portal.MapGet("/sign-up", async (HttpContext http, CustomerOperationsApi api, IOptions<CustomerBotCheckOptions> bot) =>
            Tenant(http) is { } tenant
                ? Page<SignUpPage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Language = Language(), BotSiteKey = SiteKey(bot.Value) })
                : Results.NotFound());

        portal.MapPost("/sign-up", async (HttpContext http, [FromForm] string? firstName, [FromForm] string? lastName, [FromForm] string? companyName, [FromForm] string? email,
            [FromForm] string? phone, [FromForm] string? language, [FromForm] string? password, CustomerOperationsApi api, ICustomerBotCheck botCheck,
            IOptions<CustomerBotCheckOptions> bot, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");
            string? error = null;

            // The bot check runs before anything is sent (ADR-0019).
            if (!await botCheck.VerifyAsync(http.Request.Form[BotField].ToString(), http.Connection.RemoteIpAddress?.ToString(), http.RequestAborted))
            {
                error = text["ui.error.bot"].Value;
            }
            else
            {
                CustomerCall<object> call = await api.SignUpAsync(tenant, new CustomerSignUpBody(firstName ?? string.Empty, lastName ?? string.Empty, companyName, email ?? string.Empty,
                    phone ?? string.Empty, language ?? "en", password ?? string.Empty, Slug(http)), http, http.RequestAborted);

                if (call.Succeeded)
                {
                    return Results.Redirect("/customer/sign-up/sent");
                }

                error = Describe(text, call);
            }

            return Page<SignUpPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                FirstName = firstName,
                LastName = lastName,
                CompanyName = companyName,
                Email = email,
                Phone = phone,
                Language = language is "fr" or "es" ? language : "en",
                BotSiteKey = SiteKey(bot.Value),
                Error = error
            });
        }).RequireRateLimiting(CustomerRateLimits.SignUp);

        portal.MapGet("/sign-up/sent", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await MessageAsync(http, api, text["ui.sign_up.sent.title"], text["ui.sign_up.sent.body"], "/customer/sign-in", text["ui.sign_in.link"]));

        portal.MapGet("/verify", async (HttpContext http, string? token, CustomerOperationsApi api) =>
            Tenant(http) is { } tenant ? Page<VerifyPage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Token = token ?? string.Empty }) : Results.NotFound());

        portal.MapPost("/verify", async (HttpContext http, [FromForm] string? token, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");
            CustomerCall<CustomerSessionDto> call = await api.VerifyAsync(tenant, token ?? string.Empty, http, http.RequestAborted);

            if (call.Value is { } session)
            {
                await StartSessionAsync(http, api, tenant, session.SessionToken);
                return Results.Redirect("/customer/?done=welcome");
            }

            return await MessageAsync(http, api, text["ui.link.invalid.title"], Describe(text, call), "/customer/sign-in", text["ui.sign_in.link"]);
        }).RequireRateLimiting(CustomerRateLimits.Link);

        portal.MapPost("/verify/resend", async (HttpContext http, [FromForm] string? email, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");
            await api.ResendVerificationAsync(tenant, new CustomerEmailBody(email ?? string.Empty, Slug(http)), http, http.RequestAborted);
            return Results.Redirect("/customer/sign-up/sent");
        }).RequireRateLimiting(CustomerRateLimits.Email);

        portal.MapGet("/forgot-password", async (HttpContext http, CustomerOperationsApi api) =>
            Tenant(http) is { } tenant ? Page<ForgotPasswordPage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted) }) : Results.NotFound());

        portal.MapPost("/forgot-password", async (HttpContext http, [FromForm] string? email, CustomerOperationsApi api) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");
            await api.RequestPasswordResetAsync(tenant, new CustomerEmailBody(email ?? string.Empty, Slug(http)), http, http.RequestAborted);
            return Results.Redirect("/customer/forgot-password/sent");
        }).RequireRateLimiting(CustomerRateLimits.Email);

        portal.MapGet("/forgot-password/sent", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await MessageAsync(http, api, text["ui.forgot.sent.title"], text["ui.forgot.sent.body"], "/customer/sign-in", text["ui.sign_in.link"]));

        portal.MapGet("/reset-password", async (HttpContext http, string? token, CustomerOperationsApi api) =>
            Tenant(http) is { } tenant ? Page<ResetPasswordPage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Token = token ?? string.Empty }) : Results.NotFound());

        portal.MapPost("/reset-password", async (HttpContext http, [FromForm] string? token, [FromForm] string? password, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");
            CustomerCall<CustomerSessionDto> call = await api.ResetPasswordAsync(tenant, new CustomerPasswordResetBody(token ?? string.Empty, password ?? string.Empty), http, http.RequestAborted);

            if (call.Value is { } session)
            {
                await StartSessionAsync(http, api, tenant, session.SessionToken);
                return Results.Redirect("/customer/?done=saved");
            }

            // A weak password may be tried again with the same link; a dead link may not.
            return call.ErrorCode is { } code && code.StartsWith("ops.auth.password", StringComparison.Ordinal)
                ? Page<ResetPasswordPage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Token = token ?? string.Empty, Error = Describe(text, call) })
                : await MessageAsync(http, api, text["ui.link.invalid.title"], Describe(text, call), "/customer/forgot-password", text["ui.forgot.link"]);
        }).RequireRateLimiting(CustomerRateLimits.Link);

        portal.MapGet("/profile", async (HttpContext http, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await ProfileAsync(http, api, text, null, done is null ? null : text["ui.done.saved"].Value));

        portal.MapPost("/profile", async (HttpContext http, [FromForm] string? firstName, [FromForm] string? lastName, [FromForm] string? companyName, [FromForm] string? phone,
            [FromForm] string? language, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");

            if (CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<CustomerProfileDto> call = await api.UpdateMeAsync(tenant, session,
                new CustomerProfileBody(firstName ?? string.Empty, lastName ?? string.Empty, companyName, phone ?? string.Empty, language ?? "en"), http, http.RequestAborted);

            if (call.Value is { } customer)
            {
                // The pages follow the chosen language at once.
                http.Response.Cookies.Append(CustomerCookies.Culture, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(customer.Language)),
                    new CookieOptions { Path = CustomerCookies.Path, Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(365) });
                return Results.Redirect("/customer/profile?done=1");
            }

            return await ProfileAsync(http, api, text, Describe(text, call), null);
        });

        portal.MapPost("/profile/password", async (HttpContext http, [FromForm] string? currentPassword, [FromForm] string? newPassword, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");

            if (CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<object> call = await api.ChangePasswordAsync(tenant, session, new CustomerPasswordChangeBody(currentPassword ?? string.Empty, newPassword ?? string.Empty), http, http.RequestAborted);
            return call.Succeeded ? Results.Redirect("/customer/profile?done=1") : await ProfileAsync(http, api, text, Describe(text, call), null);
        }).RequireRateLimiting(CustomerRateLimits.SignIn);

        portal.MapGet("/culture/{code}", (HttpContext http, string code, string? returnUrl) =>
        {
            if (code is "en" or "fr" or "es")
            {
                http.Response.Cookies.Append(CustomerCookies.Culture, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(code)),
                    new CookieOptions { Path = CustomerCookies.Path, Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(365) });
            }

            // Only addresses of this portal: never an open redirect.
            return Results.Redirect(returnUrl is { Length: > 1 } && returnUrl.StartsWith("/customer/", StringComparison.Ordinal) && !returnUrl.Contains("//", StringComparison.Ordinal)
                && !returnUrl.Contains('\\', StringComparison.Ordinal) ? returnUrl : "/customer/");
        });

        return endpoints;
    }

    // A new session replaces the one in the cookie, which ends at Operations too (review L7).
    private static async Task StartSessionAsync(HttpContext http, CustomerOperationsApi api, Guid tenant, string token)
    {
        if (CustomerSessionCookie.Read(http.Request) is { } old && old != token)
        {
            await api.SignOutAsync(tenant, old, http, http.RequestAborted);
        }

        CustomerSessionCookie.Write(http.Response, token);
    }

    private static async Task<IResult> ProfileAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, string? error, string? info)
    {
        Guid tenant = Tenant(http) ?? throw new InvalidOperationException("No tenant.");

        if (CustomerSessionCookie.Read(http.Request) is not { } session
            || (await api.GetMeAsync(tenant, session, http, http.RequestAborted)).Value is not { } customer)
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        return Page<ProfilePage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Customer = customer, Error = error, Info = info });
    }

    private static async Task<IResult> MessageAsync(HttpContext http, CustomerOperationsApi api, string title, string? body, string link, string linkText) =>
        Tenant(http) is { } tenant
            ? Page<MessagePage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Title = title, Body = body, LinkHref = link, LinkText = linkText })
            : Results.NotFound();

    // The text of an Operations error; a code without text, or an outage, gets a general message.
    internal static string Describe<T>(IStringLocalizer<CustomerText> text, CustomerCall<T> call)
    {
        if (call.Unavailable || call.ErrorCode is not { } code)
        {
            return text["ui.error.unavailable"].Value;
        }

        if (text[code].ResourceNotFound)
        {
            return text["ui.error.general"].Value;
        }

        // Named values only (L-10), for example {minLength}.
        return VixlyText.Format(text, code, call.Arguments?.ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal));
    }

    private static Guid? Tenant(HttpContext http) => CustomerTenantMiddleware.Host(http)?.TenantId;

    private static string Slug(HttpContext http) => CustomerTenantMiddleware.Host(http)?.CanonicalSlug ?? string.Empty;

    private static string Language() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "fr" or "es"
        ? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        : "en";

    private static string? SiteKey(CustomerBotCheckOptions options) => options.Enabled && !string.IsNullOrWhiteSpace(options.SiteKey) ? options.SiteKey : null;

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent => new RazorComponentResult<TComponent>(parameters);
}
