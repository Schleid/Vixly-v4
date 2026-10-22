using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Internal.Devices;
using Vixly.Platform.Api.Internal;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.CompletePlatformDeviceLink;
using Vixly.Platform.Application.UseCases.Licensing.CompletePlatformSecondFactorReset;
using Vixly.Platform.Application.UseCases.Licensing.CreatePlatformDeviceChallenge;
using Vixly.Platform.Application.UseCases.Licensing.DenyPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.ReleasePlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.RequestPlatformFirstDeviceLink;
using Vixly.Platform.Application.UseCases.Licensing.VerifyPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.WithdrawPlatformDeviceRelease;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantDeviceSeats;
using Vixly.Platform.Application.UseCases.TenantAccount.ListTenantDevices;
using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalDeviceEndpoints.cs
//
// PURPOSE:
// The device calls of Slice 7 and the tenant's device list, for Operations (calls 2 to 11 and 24).
//
// RESPONSIBILITIES:
// - Map each device call with the tenant context from the token
// - Keep device checks to 60 per minute per device
//
// IMPORTANT NOTES:
// - Role and step-up rules are in the use cases (D-K8)
// - A positive device check may be kept 5 minutes; refusals never (D-L5)
// =========================================================
public static class PlatformInternalDeviceEndpoints
{
    private const int PositiveCheckSeconds = 300;

    public static void Map(RouteGroupBuilder devices)
    {
        ArgumentNullException.ThrowIfNull(devices);

        // The company's licences: bought, in use, a decrease scheduled (owner, 2026-10-21: the Licences tab).
        devices.MapGet("/seats", async (GetTenantDeviceSeatsUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetTenantDeviceSeatsResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new GetTenantDeviceSeatsRequest(), cancellationToken);
            return response.Succeeded
                ? Results.Ok(new PlatformDeviceSeatsInfo(response.Seats, response.InUse, response.ScheduledSeats, response.ScheduledUtc))
                : errors.InternalProblem(response.Error!);
        });

        devices.MapGet(string.Empty, async (string? status, string? search, int? page, int? pageSize, ListTenantDevicesUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            PlatformDeviceStatus? filter = PlatformWireCodes.Parse<PlatformDeviceStatus>(status);

            if (status is not null && filter is null)
            {
                return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
            }

            ListTenantDevicesResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new ListTenantDevicesRequest { Status = filter, Search = search, Page = page ?? 1, PageSize = pageSize ?? 25 }, cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Page(response.Devices!, PlatformInternalMapping.Device)) : errors.InternalProblem(response.Error!);
        });

        devices.MapPost(string.Empty, async (EnrolPlatformDeviceBody body, EnrolPlatformDeviceUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformDeviceKind>(body.Kind) is not { } kind)
            {
                return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
            }

            EnrolPlatformDeviceResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new EnrolPlatformDeviceRequest { Name = body.Name, Kind = kind, PublicKey = body.PublicKey, Origin = new PlatformDeviceOrigin(body.Ip, body.UserAgent, body.Country) },
                cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Enrolment(response.Enrolment!)) : errors.InternalProblem(response.Error!);
        });

        devices.MapPost("/{deviceId:guid}/approve", async (Guid deviceId, ApprovePlatformDeviceUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            ApprovePlatformDeviceResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new ApprovePlatformDeviceRequest { DeviceId = deviceId }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });

        devices.MapPost("/{deviceId:guid}/deny", async (Guid deviceId, DenyPlatformDeviceUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            DenyPlatformDeviceResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new DenyPlatformDeviceRequest { DeviceId = deviceId }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });

        devices.MapPost("/{deviceId:guid}/release", async (Guid deviceId, ReleasePlatformDeviceBody body, ReleasePlatformDeviceUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformDeviceReason>(body.Reason) is not { } reason)
            {
                return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
            }

            ReleasePlatformDeviceResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new ReleasePlatformDeviceRequest { DeviceId = deviceId, Reason = reason }, cancellationToken);
            return response.Succeeded ? Results.Ok(new PlatformDeviceReleaseInfo(PlatformWireCodes.Of(response.Status), response.AwaitsReview)) : errors.InternalProblem(response.Error!);
        });

        devices.MapPost("/{deviceId:guid}/withdraw-release", async (Guid deviceId, WithdrawPlatformDeviceReleaseUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            WithdrawPlatformDeviceReleaseResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new WithdrawPlatformDeviceReleaseRequest { DeviceId = deviceId }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });

        devices.MapPost("/first-device-link", async (RequestPlatformFirstDeviceLinkUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            RequestPlatformFirstDeviceLinkResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new RequestPlatformFirstDeviceLinkRequest(), cancellationToken);
            return response.Succeeded ? Results.Accepted() : errors.InternalProblem(response.Error!);
        });

    }

    public static void MapChecks(RouteGroupBuilder device)
    {
        ArgumentNullException.ThrowIfNull(device);

        device.MapPost("/verify", async (Guid deviceId, VerifyPlatformDeviceBody body, VerifyPlatformDeviceUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            VerifyPlatformDeviceResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new VerifyPlatformDeviceRequest
                {
                    DeviceId = deviceId, Secret = body.Secret, Challenge = body.Challenge, Signature = body.Signature, Rotate = body.Rotate != false, Country = body.Country
                }, cancellationToken);

            if (!response.Succeeded)
            {
                return errors.InternalProblem(response.Error!);
            }

            // A positive answer without a new secret may be kept; a refusal or a rotation never.
            return response.Check!.Allowed && response.Check.NewSecret is null
                ? PlatformInternalCache.Ok(http, PlatformInternalMapping.Check(response.Check), PositiveCheckSeconds)
                : Results.Ok(PlatformInternalMapping.Check(response.Check));
        });

        device.MapPost("/challenge", async (Guid deviceId, CreatePlatformDeviceChallengeUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            CreatePlatformDeviceChallengeResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new CreatePlatformDeviceChallengeRequest { DeviceId = deviceId }, cancellationToken);
            return response.Succeeded ? Results.Ok(new PlatformDeviceChallengeInfo(response.Challenge)) : errors.InternalProblem(response.Error!);
        });
    }

    public static void MapLinks(RouteGroupBuilder links)
    {
        ArgumentNullException.ThrowIfNull(links);

        // The emailed link proves the tenant; the user may not be signed in yet (D-L7, D-L8).
        links.MapPost("/complete", async (CompletePlatformDeviceLinkBody body, CompletePlatformDeviceLinkUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformDeviceKind>(body.Kind) is not { } kind)
            {
                return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
            }

            CompletePlatformDeviceLinkResponse response = await useCase.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
                new CompletePlatformDeviceLinkRequest { Token = body.Token, Name = body.Name, Kind = kind, PublicKey = body.PublicKey, ReplaceDeviceId = body.ReplaceDeviceId, ExpectedTenantId = PlatformInternalRequest.Find(http)?.TenantId,
                    Origin = new PlatformDeviceOrigin(body.Ip, body.UserAgent, body.Country) }, cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Enrolment(response.Enrolment!)) : errors.InternalProblem(response.Error!);
        });

        // Owner second-factor reset started by Vixly support (D-O13); Operations then enrols a new second factor.
        links.MapPost("/second-factor-reset/complete", async (CompletePlatformSecondFactorResetBody body, CompletePlatformSecondFactorResetUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            CompletePlatformSecondFactorResetResponse response = await useCase.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
                new CompletePlatformSecondFactorResetRequest { Token = body.Token, ExpectedTenantId = PlatformInternalRequest.Find(http)?.TenantId }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });
    }
}
