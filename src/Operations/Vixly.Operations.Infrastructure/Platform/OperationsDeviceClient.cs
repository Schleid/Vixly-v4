using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Internal.Devices;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsDeviceClient.cs
//
// PURPOSE:
// Calls Platform's device endpoints for Operations (Slice 1C).
//
// RESPONSIBILITIES:
// - Sign each call for the tenant, and for the acting user when there is one
// - Translate Platform refusals to ops.devices.* errors
//
// IMPORTANT NOTES:
// - Platform down or a malformed answer throws OperationsPlatformUnavailableException (the page says "try again")
// - Calls go through OperationsPlatformSender
// - A device check is never cached here; the web host keeps positive answers 5 minutes
// =========================================================
public sealed class OperationsDeviceClient : IOperationsDeviceClient
{
    private const int MaxDevicePages = 20;

    private readonly OperationsPlatformSender _sender;

    public OperationsDeviceClient(OperationsPlatformSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<OperationsPlatformCall<bool>> RequestFirstDeviceLinkAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return Done(await SendAsync<object>(HttpMethod.Post, "internal/v1/tenant/devices/first-device-link", actor.TenantId, actor, null, cancellationToken));
    }

    public async Task<OperationsPlatformCall<OperationsDeviceEnrolment>> CompleteLinkAsync(Guid tenantId, string token, string name, string kind, OperationsDeviceOrigin origin,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(origin);
        (PlatformDeviceEnrolmentInfo? value, Error? error) = await SendAsync<PlatformDeviceEnrolmentInfo>(
            HttpMethod.Post, "internal/v1/device-links/complete", tenantId, null, new CompletePlatformDeviceLinkBody(token, name, kind, null, null, origin.Ip, origin.UserAgent, origin.Country),
            cancellationToken);
        return Enrolment(value, error);
    }

    public async Task<OperationsPlatformCall<OperationsDeviceEnrolment>> EnrolAsync(OperationsPlatformActor actor, string name, string kind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformDeviceEnrolmentInfo? value, Error? error) = await SendAsync<PlatformDeviceEnrolmentInfo>(
            HttpMethod.Post, "internal/v1/tenant/devices", actor.TenantId, actor, new EnrolPlatformDeviceBody(name, kind, null, actor.ClientIp, actor.UserAgent, actor.Country),
            cancellationToken);
        return Enrolment(value, error);
    }

    public async Task<OperationsPlatformCall<bool>> CompleteSecondFactorResetAsync(Guid tenantId, string token, CancellationToken cancellationToken)
    {
        return Done(await SendAsync<object>(HttpMethod.Post, "internal/v1/device-links/second-factor-reset/complete", tenantId, null, new { token }, cancellationToken));
    }

    public async Task<OperationsDeviceCheck> VerifyAsync(Guid tenantId, Guid deviceId, string secret, bool rotate, string? country, CancellationToken cancellationToken)
    {
        (PlatformDeviceCheckInfo? value, Error? error) = await SendAsync<PlatformDeviceCheckInfo>(
            HttpMethod.Post, $"internal/v1/tenant/devices/{deviceId:D}/verify", tenantId, null, new VerifyPlatformDeviceBody(secret, null, null, rotate, country), cancellationToken);

        // A device of another tenant or one Platform never heard of is simply unknown.
        return value is not null
            ? new OperationsDeviceCheck(value.Allowed, value.Code, value.Access, value.NewSecret, value.Name, value.Number, value.Country)
            : new OperationsDeviceCheck(false, error?.Code == OperationsDeviceErrors.NotAllowed.Code ? "not_allowed" : "device_unknown", "none", null);
    }

    public async Task<OperationsPlatformCall<IReadOnlyList<OperationsDevice>>> ListAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        List<PlatformDeviceInfo> items = new();

        // Every page (owner, 2026-10-21: each licence and its device); at most MaxDevicePages of 100.
        for (int number = 1; number <= MaxDevicePages; number++)
        {
            (PlatformPageInfo<PlatformDeviceInfo>? page, Error? error) = await SendAsync<PlatformPageInfo<PlatformDeviceInfo>>(
                HttpMethod.Get, $"internal/v1/tenant/devices?page={number}&pageSize=100", actor.TenantId, actor, null, cancellationToken);

            if (error is not null || page is null)
            {
                return OperationsPlatformCall<IReadOnlyList<OperationsDevice>>.Refused(error ?? OperationsDeviceErrors.Refused);
            }

            items.AddRange(page.Items);

            if (page.Items.Count == 0 || number * page.PageSize >= page.TotalCount)
            {
                break;
            }
        }

        return OperationsPlatformCall<IReadOnlyList<OperationsDevice>>.Ok(items
                .Select(item => new OperationsDevice(item.DeviceId, item.Number, item.Name, item.Kind, item.Status, item.SeatHeld, item.RequestedByName, item.RequestedUtc, item.LastSeenUtc,
                    item.Browser, item.ApprovedUtc, item.ApprovedByName))
                .ToList());
    }

    public async Task<OperationsPlatformCall<OperationsDeviceSeats>> SeatsAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformDeviceSeatsInfo? value, Error? error) = await SendAsync<PlatformDeviceSeatsInfo>(HttpMethod.Get, "internal/v1/tenant/devices/seats", actor.TenantId, actor, null, cancellationToken);

        return error is null && value is not null
            ? OperationsPlatformCall<OperationsDeviceSeats>.Ok(new OperationsDeviceSeats(value.Seats, value.InUse, value.ScheduledSeats, value.ScheduledUtc))
            : OperationsPlatformCall<OperationsDeviceSeats>.Refused(error ?? OperationsDeviceErrors.Refused);
    }

    public async Task<OperationsPlatformCall<bool>> ApproveAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return Done(await SendAsync<object>(HttpMethod.Post, $"internal/v1/tenant/devices/{deviceId:D}/approve", actor.TenantId, actor, null, cancellationToken));
    }

    public async Task<OperationsPlatformCall<bool>> DenyAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return Done(await SendAsync<object>(HttpMethod.Post, $"internal/v1/tenant/devices/{deviceId:D}/deny", actor.TenantId, actor, null, cancellationToken));
    }

    public async Task<OperationsPlatformCall<bool>> ReleaseAsync(OperationsPlatformActor actor, Guid deviceId, string reason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return Done(await SendAsync<object>(HttpMethod.Post, $"internal/v1/tenant/devices/{deviceId:D}/release", actor.TenantId, actor, new ReleasePlatformDeviceBody(reason), cancellationToken));
    }

    // Platform's codes, as Operations names them; anything else is a plain refusal.
    public static Error Translate(string? code) => code switch
    {
        "platform.licensing.link_invalid" => OperationsDeviceErrors.LinkInvalid,
        "platform.licensing.registration_not_ready" or "platform.licensing.tenant_not_active" => OperationsDeviceErrors.NotReady,
        "platform.licensing.too_many_links" => OperationsDeviceErrors.TooManyLinks,
        "platform.licensing.no_free_seat" => OperationsDeviceErrors.NoFreeSeat,
        "platform.licensing.too_many_pending" => OperationsDeviceErrors.TooManyPending,
        "platform.licensing.name_invalid" => OperationsDeviceErrors.NameRequired,
        "platform.licensing.kind_invalid" => OperationsDeviceErrors.KindInvalid,
        "platform.common.role_not_allowed" or "platform.common.step_up_required" => OperationsDeviceErrors.NotAllowed,
        _ => OperationsDeviceErrors.Refused
    };

    private static OperationsPlatformCall<bool> Done((object? Value, Error? Error) result) =>
        result.Error is null ? OperationsPlatformCall<bool>.Ok(true) : OperationsPlatformCall<bool>.Refused(result.Error);

    private static OperationsPlatformCall<OperationsDeviceEnrolment> Enrolment(PlatformDeviceEnrolmentInfo? value, Error? error) =>
        error is null && value is not null
            ? OperationsPlatformCall<OperationsDeviceEnrolment>.Ok(new OperationsDeviceEnrolment(value.DeviceId, value.Number, value.Status, value.Secret))
            : OperationsPlatformCall<OperationsDeviceEnrolment>.Refused(error ?? OperationsDeviceErrors.Refused);

    private async Task<(T? Value, Error? Error)> SendAsync<T>(HttpMethod method, string path, Guid tenantId, OperationsPlatformActor? actor, object? body, CancellationToken cancellationToken)
        where T : class
    {
        (T? value, string? refused) = await _sender.SendAsync<T>(method, path, tenantId, actor, body, cancellationToken);
        return (value, refused is null ? null : Translate(refused));
    }
}
