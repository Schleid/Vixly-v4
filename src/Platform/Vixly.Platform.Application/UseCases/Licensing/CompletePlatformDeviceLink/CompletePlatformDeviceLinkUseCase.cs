using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.CompletePlatformDeviceLink;

// =========================================================
// FILE: CompletePlatformDeviceLinkUseCase.cs
//
// PURPOSE:
// Enrols the device that opened a first-device or recovery link; the first device completes the registration and activates the tenant (D-L7, D-L8).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the page on the tenant host posts the token from after "#" (rate-limited, S-6)
// - One answer for every unusable link; 5 tries per link
// =========================================================
public sealed class CompletePlatformDeviceLinkUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public CompletePlatformDeviceLinkUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<CompletePlatformDeviceLinkResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompletePlatformDeviceLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _licensing.UtcNow;
        PlatformDeviceLink? link = string.IsNullOrEmpty(request.Token) ? null : await _licensing.Store.GetLinkAsync(_licensing.Hash(request.Token), cancellationToken);

        if (link is null || link.Kind == PlatformDeviceLinkKind.SecondFactorReset || (request.ExpectedTenantId is { } expected && link.PlatformTenantId != expected) || !link.Try(utcNow))
        {
            if (link is not null)
            {
                await _licensing.SaveAsync(cancellationToken);
            }

            return CompletePlatformDeviceLinkResponse.Failure(PlatformLicensingErrors.LinkInvalid);
        }

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(link.PlatformTenantId, cancellationToken);
        PlatformSubscription? subscription = await _licensing.Subscriptions.Subscriptions.GetCurrentAsync(link.PlatformTenantId, cancellationToken);
        bool keyed = !string.IsNullOrWhiteSpace(request.PublicKey);

        if (tenant is null || (keyed && !_licensing.Keys.IsValidPublicKey(request.PublicKey)))
        {
            await _licensing.SaveAsync(cancellationToken);
            return CompletePlatformDeviceLinkResponse.Failure(tenant is null ? PlatformLicensingErrors.LinkInvalid : PlatformLicensingErrors.PublicKeyInvalid);
        }

        PlatformTenantLicensing seats = await _licensing.SeatsAsync(tenant.Id, cancellationToken);
        PlatformOwnerSupportRequest? support = link.SupportRequestId is Guid supportId ? await _licensing.Store.GetSupportRequestAsync(supportId, cancellationToken) : null;

        // The device support named when it started the recovery gives its seat, unless the page names another (D-O13).
        Guid? replaceDeviceId = request.ReplaceDeviceId ?? support?.ReplaceDeviceId;
        PlatformSecret? secret = keyed ? null : _licensing.NewSecret();
        PlatformDevice device;
        PlatformDeviceEvent requested;
        int limit = PlatformLicensingAdministration.SeatLimit(subscription);

        try
        {
            (device, requested) = PlatformDevice.Enrol(tenant.Id, seats.NextNumber(utcNow), request.Name, request.Kind, keyed ? request.PublicKey : null, secret?.Hash, PlatformDeviceActor.System, utcNow);
            _licensing.Store.AddDevice(device);
            _licensing.Store.AddEvents(new[] { requested });

            if (link.Kind == PlatformDeviceLinkKind.OwnerRecovery && seats.SeatsInUse >= limit)
            {
                // No free seat: the device the owner names gives its seat to the new one, in this commit.
                PlatformDevice? replaced = replaceDeviceId is Guid replaceId ? await _licensing.Store.GetDeviceAsync(replaceId, cancellationToken) : null;

                if (replaced is null || replaced.PlatformTenantId != tenant.Id)
                {
                    await _licensing.SaveAsync(cancellationToken);
                    return CompletePlatformDeviceLinkResponse.Failure(PlatformLicensingErrors.NoFreeSeat(seats.SeatsInUse, limit));
                }

                _licensing.Record(replaced, replaced.Revoke(PlatformDeviceReason.ReplacedByRecovery, PlatformDeviceActor.System, utcNow), seats, limit, null, "platform.licensing.device_revoked", null);
            }

            _licensing.Record(device, device.Approve(PlatformDeviceActor.System, utcNow, PlatformDeviceEventCodes.EnrolledByLink), seats, limit, null, "platform.licensing.device_enrolled_by_link", null);
        }
        catch (DomainException exception)
        {
            _licensing.UnitOfWork.DiscardChanges();
            return CompletePlatformDeviceLinkResponse.Failure(exception.Error);
        }

        link.MarkUsed(utcNow);

        if (support is { Status: PlatformOwnerSupportStatus.Approved })
        {
            support.Complete(utcNow);
        }

        if (link.Kind == PlatformDeviceLinkKind.FirstDevice)
        {
            // One commit: registration complete, tenant active, first device in use (D-L7).
            if (link.PlatformTenantRegistrationId is Guid registrationId && await _licensing.Registrations.GetAsync(registrationId, cancellationToken) is { } registration)
            {
                registration.CompleteWithFirstDevice(utcNow);
            }

            if (tenant.ActivateFromRegistration(utcNow) is { } activated)
            {
                _licensing.RecordTenantActivated(tenant, activated);

                // The onboarding is complete: the welcome email is the last one (owner, 2026-10-18).
                _licensing.Notice(tenant.Id, PlatformLicensingNoticeKind.Welcome);
            }
        }
        else
        {
            _licensing.Notice(tenant.Id, PlatformLicensingNoticeKind.DeviceReplaced, device.Id);
        }

        return await _licensing.SaveAsync(cancellationToken)
            ? CompletePlatformDeviceLinkResponse.Success(new PlatformDeviceEnrolment(device.Id, device.Number, device.Status, secret?.Value))
            : CompletePlatformDeviceLinkResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
