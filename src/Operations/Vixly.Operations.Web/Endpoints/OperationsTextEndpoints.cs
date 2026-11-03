using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Texts.ConnectOperationsTextProvider;
using Vixly.Operations.Application.UseCases.Texts.DisconnectOperationsTextProvider;
using Vixly.Operations.Application.UseCases.Texts.GetOperationsTextSettings;
using Vixly.Operations.Application.UseCases.Texts.HandleOperationsTextStatus;
using Vixly.Operations.Application.UseCases.Texts.SaveOperationsTextNotices;
using Vixly.Operations.Domain.Texts;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Texts;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsTextEndpoints.cs
//
// PURPOSE:
// Settings → Messages (Slice 9): the company's Twilio account and the notices it sends by SMS or WhatsApp.
//
// RESPONSIBILITIES:
// - /staff/settings/messages and its forms: connect or change the account, disconnect, the notices and WhatsApp templates
// - POST /hooks/twilio: Twilio's report of each message, trusted only when signed with the company's token
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); connecting and disconnecting ask for the second check
// - The hook is anonymous: no antiforgery (Twilio signs it), at most 16 KB, rate-limited per host
// =========================================================
public static class OperationsTextEndpoints
{
    private const int MaxHookBytes = 16 * 1024;

    // The company's own address for Twilio's reports, as Twilio will call it (the signature covers it).
    public static string CallbackUrl(HttpContext http) => "https://" + (http ?? throw new ArgumentNullException(nameof(http))).Request.Host.Value + "/hooks/twilio";

    public static IEndpointRouteBuilder MapOperationsTextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/settings/messages").RequireAuthorization().RequireFormToken();

        staff.MapGet("", async (HttpContext http, string? done, GetOperationsTextSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) => await PageAsync(http, null, done is null ? null : text["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/connect", async (HttpContext http, [FromForm] string? accountSid, [FromForm] string? authToken, [FromForm] string? smsFrom, [FromForm] string? whatsAppFrom,
            ConnectOperationsTextProviderUseCase connect, GetOperationsTextSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ConnectOperationsTextProviderResponse response = await connect.ExecuteAsync(OperationsCurrentUser.Context(http), new ConnectOperationsTextProviderRequest
            {
                AccountSid = accountSid,
                AuthToken = authToken,
                SmsFrom = smsFrom,
                WhatsAppFrom = whatsAppFrom,
                CallbackUrl = CallbackUrl(http)
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/messages?done=1")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/settings/messages") ?? await PageAsync(http, response.Error, null, get, brand, errors);
        });

        endpoints.MapPost("/hooks/twilio", async (HttpContext http, HandleOperationsTextStatusUseCase handle) =>
        {
            if (http.Request.ContentLength is null or > MaxHookBytes || !http.Request.HasFormContentType)
            {
                return Results.BadRequest();
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            HandleOperationsTextStatusResponse response = await handle.ExecuteAsync(OperationsCurrentUser.Context(http), new HandleOperationsTextStatusRequest
            {
                Url = "https://" + http.Request.Host.Value + http.Request.Path.Value + http.Request.QueryString.Value,
                Form = form.SelectMany(field => field.Value.Select(value => new KeyValuePair<string, string>(field.Key, value ?? string.Empty))).ToArray(),
                Signature = http.Request.Headers["X-Twilio-Signature"].ToString()
            }, http.RequestAborted);
            return response.Succeeded ? Results.Ok() : Results.StatusCode(response.Error == OperationsTextErrors.ProviderRefused ? StatusCodes.Status403Forbidden : StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous().DisableAntiforgery().RequireRateLimiting(OperationsRateLimits.TextWebhook);

        staff.MapPost("/disconnect", async (HttpContext http, DisconnectOperationsTextProviderUseCase disconnect, GetOperationsTextSettingsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            DisconnectOperationsTextProviderResponse response = await disconnect.ExecuteAsync(OperationsCurrentUser.Context(http), new DisconnectOperationsTextProviderRequest(),
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/messages?done=1")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/settings/messages") ?? await PageAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/notices", async (HttpContext http, SaveOperationsTextNoticesUseCase save, GetOperationsTextSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            OperationsTextNotices notices = OperationsTextNotices.None;

            foreach (string? value in form["notice"])
            {
                if (Enum.TryParse(value, false, out OperationsTextNotices notice) && Enum.IsDefined(notice))
                {
                    notices |= notice;
                }
            }

            Dictionary<OperationsTextNotices, string?> templates = Enum.GetValues<OperationsTextNotices>().Where(notice => notice != OperationsTextNotices.None)
                .ToDictionary(notice => notice, notice => (string?)form["template_" + notice].ToString());
            SaveOperationsTextNoticesResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SaveOperationsTextNoticesRequest { Notices = notices, Templates = templates }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/messages?done=1#notices") : await PageAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> PageAsync(HttpContext http, Error? error, string? info, GetOperationsTextSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsTextSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsTextSettingsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<TextSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.InPlan,
            response.Provider,
            response.Notices,
            response.Messages,
            response.CanEdit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
