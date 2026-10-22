using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Application.UseCases.Devices.VerifyOperationsDevice;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Hosting;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsDeviceGate.cs
//
// PURPOSE:
// Lets a staff session of a live tenant reach the workspace only from a licensed device (Slice 1C, Slice 7).
//
// RESPONSIBILITIES:
// - Check the device cookie with Platform; keep a positive answer 5 minutes
// - Write the new secret when Platform rotates it
// - Send unknown devices to enrolment and waiting devices to the waiting page
//
// IMPORTANT NOTES:
// - Only signed-in staff on /staff pages of a live tenant: sign-in steps, password reset, device pages, the public site and setup are not gated
// - A refusal is never kept (D-L5); Platform down answers "try again"
// =========================================================
public sealed class OperationsDeviceGate
{
    private static readonly TimeSpan PositiveCheck = TimeSpan.FromMinutes(5);

    private static readonly string[] OpenStaffPaths = { "/staff/sign-in", "/staff/sign-out", "/staff/forgot-password", "/staff/reset-password" };

    private readonly RequestDelegate _next;

    public OperationsDeviceGate(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext http, VerifyOperationsDeviceUseCase verify, IMemoryCache cache, IHostEnvironment environment,
        Microsoft.Extensions.Options.IOptions<Vixly.Operations.Web.Hosting.OperationsDemoDataOptions> demo)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(verify);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(demo);

        // Development only: the demo staff have no licensed computer (OperationsDemoData:SkipDeviceGate).
        if (!Gated(http) || (environment.IsDevelopment() && demo.Value.SkipDeviceGate))
        {
            await _next(http);
            return;
        }

        if (OperationsDeviceCookie.Read(http.Request) is not { } device)
        {
            http.Response.Redirect("/devices/enrol");
            return;
        }

        // The country and the session are part of the key: a positive answer holds only where and for whom it was given (owner, 2026-10-21).
        string key = "ops-device:" + OperationsTenantMiddleware.Host(http)!.TenantId + ":" + device.DeviceId + ":" + Hash(device.Secret) + ":"
            + VixlyClientCountry.Of(http) + ":" + OperationsCurrentUser.Context(http).SessionId;

        if (!cache.TryGetValue(key, out _))
        {
            VerifyOperationsDeviceResponse check;

            try
            {
                check = await verify.ExecuteAsync(OperationsCurrentUser.Context(http),
                    new VerifyOperationsDeviceRequest { DeviceId = device.DeviceId, Secret = device.Secret }, http.RequestAborted);
            }
            catch (OperationsPlatformUnavailableException)
            {
                http.Response.Redirect(OperationsTenantMiddleware.UnavailablePath);
                return;
            }

            if (!check.Allowed)
            {
                // The session ended: signed in again from here, the second factor confirms the other country.
                if (check.Code == VerifyOperationsDeviceUseCase.CountryChanged)
                {
                    OperationsCookies.Delete(http.Response, OperationsCookies.Session);
                    http.Response.Redirect("/staff?country=1");
                    return;
                }

                if (check.Code == "device_pending")
                {
                    http.Response.Redirect("/devices/waiting");
                    return;
                }

                OperationsDeviceCookie.Delete(http.Response);
                http.Response.Redirect("/devices/enrol?reason=" + Uri.EscapeDataString(check.Code ?? "device_unknown"));
                return;
            }

            if (check.NewSecret is { Length: > 0 } rotated)
            {
                OperationsDeviceCookie.Write(http.Response, device.DeviceId, rotated);
            }
            else
            {
                cache.Set(key, true, PositiveCheck);
            }
        }

        await _next(http);
    }

    private static bool Gated(HttpContext http)
    {
        PathString path = http.Request.Path;

        return OperationsCurrentUser.IsSignedIn(http)
            && OperationsTenantMiddleware.Host(http) is { State: OperationsTenantState.Live, TenantId: not null }
            && path.StartsWithSegments("/staff")
            && !OpenStaffPaths.Any(open => path.StartsWithSegments(open));
    }

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
