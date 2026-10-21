using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.UseCases.Devices.CompleteOperationsDeviceLink;
using Vixly.Operations.Application.UseCases.Devices.DecideOperationsDevice;
using Vixly.Operations.Application.UseCases.Devices.EnrolOperationsDevice;
using Vixly.Operations.Application.UseCases.Devices.ListOperationsDevices;
using Vixly.Operations.Application.UseCases.Devices.ReleaseOperationsDevice;
using Vixly.Operations.Application.UseCases.Devices.RequestOperationsFirstDeviceLink;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Web.Components.Editor;
using Vixly.Operations.Web.Components.SignIn;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsDeviceEndpoints.cs
//
// PURPOSE:
// The device pages of a tenant (Slice 1C): the first-device link, emailed links, adding a computer and the Devices screen.
//
// RESPONSIBILITIES:
// - POST /setup/first-device: the Owner asks Platform for the first-device link
// - /devices/first, /devices/recover: complete an emailed link on this computer
// - /devices/enrol, /devices/waiting: a signed-in staff member on a new computer
// - /staff/devices: list, approve, deny and stop devices
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019)
// - Platform owns devices; Operations keeps only the device cookie
// - Platform down: the "try again" page
// =========================================================
public static class OperationsDeviceEndpoints
{
    public static IEndpointRouteBuilder MapOperationsDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/setup/first-device", async (HttpContext http, RequestOperationsFirstDeviceLinkUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            RequestOperationsFirstDeviceLinkResponse response = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new RequestOperationsFirstDeviceLinkRequest(), http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/setup?link=sent")
                : Page<SetupHomePage>(new { Company = await CompanyAsync(http, brand), Name = OperationsCurrentUser.DisplayName(http), Error = text.Describe(response.Error) });
        }).RequireAuthorization().RequireRateLimiting(OperationsRateLimits.AccountLink);

        endpoints.MapGet("/devices/first", async (HttpContext http, GetOperationsSignInBrandUseCase brand) => LinkPage(await CompanyAsync(http, brand), false, null));

        endpoints.MapGet("/devices/recover", async (HttpContext http, GetOperationsSignInBrandUseCase brand) => LinkPage(await CompanyAsync(http, brand), true, null));

        endpoints.MapPost("/devices/link", async (HttpContext http, [FromForm] string? token, [FromForm] string? name, [FromForm] string? kind,
            CompleteOperationsDeviceLinkUseCase useCase, IOperationsTenantDirectory directory, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            CompleteOperationsDeviceLinkResponse response = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new CompleteOperationsDeviceLinkRequest { Token = token ?? string.Empty, Name = name ?? string.Empty, Kind = kind ?? "pc" }, http.RequestAborted));

            if (!response.Succeeded)
            {
                return LinkPage(await CompanyAsync(http, brand), false, text.Describe(response.Error));
            }

            OperationsDeviceCookie.Write(http.Response, response.DeviceId, response.Secret);

            // The first device may have just made the tenant live: ask Platform again now, not in 60 seconds.
            directory.Forget(OperationsCurrentUser.Context(http).RequiredTenantId);
            return Results.Redirect("/staff?device=ready");
        }).RequireRateLimiting(OperationsRateLimits.AccountLink);

        endpoints.MapGet("/devices/enrol", async (HttpContext http, string? reason, GetOperationsSignInBrandUseCase brand, Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText> text) =>
            Page<DeviceEnrolPage>(new { Company = await CompanyAsync(http, brand), Info = reason is null ? null : text["ui.devices.check." + Code(reason)].Value }))
            .RequireAuthorization();

        endpoints.MapPost("/devices/enrol", async (HttpContext http, [FromForm] string? name, [FromForm] string? kind, EnrolOperationsDeviceUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            EnrolOperationsDeviceResponse response = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new EnrolOperationsDeviceRequest { Name = name ?? string.Empty, Kind = kind ?? "pc" }, http.RequestAborted));

            if (!response.Succeeded)
            {
                return Page<DeviceEnrolPage>(new { Company = await CompanyAsync(http, brand), Error = text.Describe(response.Error) });
            }

            if (response.Secret.Length > 0)
            {
                OperationsDeviceCookie.Write(http.Response, response.DeviceId, response.Secret);
            }

            return Results.Redirect(response.Status == "active" ? "/staff" : "/devices/waiting");
        }).RequireAuthorization().RequireRateLimiting(OperationsRateLimits.AccountLink);

        endpoints.MapGet("/devices/waiting", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            Page<DeviceWaitingPage>(new { Company = await CompanyAsync(http, brand) })).RequireAuthorization();

        endpoints.MapGet("/staff/devices", async (HttpContext http, string? done, ListOperationsDevicesUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text,
            Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText> texts) =>
            await DevicesPageAsync(http, useCase, await CompanyAsync(http, brand), text, null, done is null ? null : texts["ui.devices.done"].Value))
            .RequireAuthorization();

        endpoints.MapPost("/staff/devices/{deviceId:guid}/decide", async (HttpContext http, Guid deviceId, [FromForm] bool approve, [FromForm] string? code,
            DecideOperationsDeviceUseCase decide, ListOperationsDevicesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            DecideOperationsDeviceResponse response = await Unavailable(() => decide.ExecuteAsync(OperationsCurrentUser.Context(http),
                new DecideOperationsDeviceRequest { DeviceId = deviceId, Approve = approve, Code = code ?? string.Empty }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/devices?done=1")
                : await DevicesPageAsync(http, list, await CompanyAsync(http, brand), text, response.Error, null);
        }).RequireAuthorization().RequireRateLimiting(OperationsRateLimits.SecondFactor);

        endpoints.MapPost("/staff/devices/{deviceId:guid}/release", async (HttpContext http, Guid deviceId, [FromForm] string? reason, [FromForm] string? code,
            ReleaseOperationsDeviceUseCase release, ListOperationsDevicesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            ReleaseOperationsDeviceResponse response = await Unavailable(() => release.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ReleaseOperationsDeviceRequest { DeviceId = deviceId, Reason = reason ?? string.Empty, Code = code ?? string.Empty }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/devices?done=1")
                : await DevicesPageAsync(http, list, await CompanyAsync(http, brand), text, response.Error, null);
        }).RequireAuthorization().RequireRateLimiting(OperationsRateLimits.SecondFactor);

        return endpoints;
    }

    // Platform being down is not the user's mistake: the page says "try again".
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

    private static async Task<IResult> DevicesPageAsync(HttpContext http, ListOperationsDevicesUseCase useCase, string? company, OperationsErrorText text, Error? error, string? info)
    {
        ListOperationsDevicesResponse devices = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsDevicesRequest(), http.RequestAborted));
        http.Response.Headers.CacheControl = "no-store";

        return Page<DevicesPage>(new
        {
            Company = company,
            Devices = devices.Devices,
            Error = text.Describe(error ?? devices.Error),
            Info = info
        });
    }

    private static IResult LinkPage(string? company, bool recovery, string? error) =>
        Page<DeviceLinkPage>(new { Company = company, Recovery = recovery, Error = error });

    // Only Platform's known codes become a text key; anything else reads as "unknown".
    private static string Code(string reason) => reason switch
    {
        "device_released" or "device_revoked" or "device_unknown" or "proof_invalid" or "tenant_suspended" or "tenant_closed" or "subscription_none" => reason,
        _ => "device_unknown"
    };

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
