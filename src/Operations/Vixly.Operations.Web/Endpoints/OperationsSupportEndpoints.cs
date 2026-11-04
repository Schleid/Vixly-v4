using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Support;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Support.FindOperationsSupportCaller;
using Vixly.Operations.Application.UseCases.Support.GetOperationsSupportCase;
using Vixly.Operations.Application.UseCases.Support.OpenOperationsSupportRequest;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Support;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsSupportEndpoints.cs
//
// PURPOSE:
// The support agent's call screen and staff's call tickets (owner, 2026-10-29, design section 9).
//
// RESPONSIBILITIES:
// - /staff/support: the caller's phone, then a tracking number, then a call ticket
// - Call tickets are tickets of the ticket desk (/staff/tickets); /staff/support/requests leads there
//
// IMPORTANT NOTES:
// - Phones and tracking numbers are posted, never put in an address (they would stay in logs and history)
// - The caller found in step 1 travels to step 2 as a protected, short-lived value tied to the agent; never as a plain id
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check permissions
// =========================================================
public static class OperationsSupportEndpoints
{
    private const string Purpose = "Vixly.Operations.SupportCaller.v1";

    private static readonly TimeSpan CallerLifetime = TimeSpan.FromMinutes(30);

    public static IEndpointRouteBuilder MapOperationsSupportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/support", async (HttpContext http, string? done, FindOperationsSupportCallerUseCase check, GetOperationsSignInBrandUseCase brand,
            IStringLocalizer<OperationsText> text, OperationsErrorText errors) =>
        {
            // Nothing typed: only who may answer calls (and when, for an outside account) is checked; nothing is looked up or recorded.
            Error? refused = (await check.ExecuteAsync(OperationsCurrentUser.Context(http), new FindOperationsSupportCallerRequest(), http.RequestAborted)).Error;

            return refused is not null && refused.Code != Vixly.Operations.Domain.Support.OperationsSupportErrors.PhoneInvalid.Code
                ? Page<SupportPage>(http, new { Company = await CompanyAsync(http, brand), Refused = true, Error = errors.Describe(refused) })
                : Page<SupportPage>(http, new { Company = await CompanyAsync(http, brand), Info = done == "request" ? text["ui.support.done.request"].Value : null });
        });

        // Step 1: the caller's phone.
        staff.MapPost("/support/caller", async (HttpContext http, [FromForm] string? phone, FindOperationsSupportCallerUseCase useCase, IDataProtectionProvider protection,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            FindOperationsSupportCallerResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new FindOperationsSupportCallerRequest { Phone = phone },
                http.RequestAborted);

            return Page<SupportPage>(http, new
            {
                Company = await CompanyAsync(http, brand),
                Asked = response.Succeeded,
                Caller = response.Caller,
                CallerToken = response.Caller is { } caller ? Protect(http, protection, caller.CustomerId) : null,
                Error = errors.Describe(response.Error)
            });
        });

        // Step 2: the tracking number, with or without a caller.
        staff.MapPost("/support/case", async (HttpContext http, [FromForm] string? caller, [FromForm] string? number, GetOperationsSupportCaseUseCase useCase,
            IDataProtectionProvider protection, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Guid? callerId = Unprotect(http, protection, caller);
            GetOperationsSupportCaseResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsSupportCaseRequest { CallerId = callerId, Number = number }, http.RequestAborted);

            return Page<SupportPage>(http, new
            {
                Company = await CompanyAsync(http, brand),
                Asked = callerId is not null,
                CallerToken = callerId is { } id ? Protect(http, protection, id) : null,
                Case = response.Case,
                Number = number,
                Error = errors.Describe(response.Error)
            });
        });

        // A call ticket for staff, once both matched.
        staff.MapPost("/support/request", async (HttpContext http, [FromForm] string? caller, [FromForm] string? number, [FromForm] string? text, [FromForm] string? topic,
            OpenOperationsSupportRequestUseCase useCase, IDataProtectionProvider protection, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Guid? callerId = Unprotect(http, protection, caller);
            OpenOperationsSupportRequestResponse response = callerId is { } id
                ? await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new OpenOperationsSupportRequestRequest
                {
                    CallerId = id, Number = number, Text = text,
                    Topic = Enum.TryParse(topic, ignoreCase: false, out Vixly.Operations.Domain.Tickets.OperationsTicketTopic chosen) && Enum.IsDefined(chosen)
                        ? chosen : Vixly.Operations.Domain.Tickets.OperationsTicketTopic.Other
                }, http.RequestAborted)
                : OpenOperationsSupportRequestResponse.Failure(Vixly.Operations.Domain.Support.OperationsSupportErrors.CallerRequired);

            return response.Succeeded
                ? Results.Redirect("/staff/support?done=request")
                : Page<SupportPage>(http, new { Company = await CompanyAsync(http, brand), Error = errors.Describe(response.Error) });
        });

        // Owner, 2026-10-29: call tickets are tickets of the desk now.
        staff.MapGet("/support/requests", () => Results.Redirect("/staff/tickets"));

        return endpoints;
    }

    // The caller's account, tied to this agent and good for half an hour.
    private static string Protect(HttpContext http, IDataProtectionProvider protection, Guid customerId) =>
        protection.CreateProtector(Purpose).ToTimeLimitedDataProtector()
            .Protect(customerId.ToString("N") + "|" + OperationsCurrentUser.Context(http).UserId?.ToString("N"), CallerLifetime);

    private static Guid? Unprotect(HttpContext http, IDataProtectionProvider protection, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            string[] parts = protection.CreateProtector(Purpose).ToTimeLimitedDataProtector().Unprotect(token).Split('|');
            return parts.Length == 2 && parts[1] == OperationsCurrentUser.Context(http).UserId?.ToString("N") && Guid.TryParseExact(parts[0], "N", out Guid id) ? id : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    private static IResult Page<TComponent>(HttpContext http, object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        http.Response.Headers.CacheControl = "no-store";
        return new RazorComponentResult<TComponent>(parameters);
    }

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand) =>
        (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
}
