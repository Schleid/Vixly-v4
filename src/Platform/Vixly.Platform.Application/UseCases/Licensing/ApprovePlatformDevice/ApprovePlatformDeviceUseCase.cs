using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;

// =========================================================
// FILE: ApprovePlatformDeviceUseCase.cs
//
// PURPOSE:
// A tenant admin approves a waiting device; it takes a free seat (D-L2, D-L4).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Role and step-up rules from the service token (Slice 9, D-K8)
// =========================================================
public sealed class ApprovePlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public ApprovePlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<ApprovePlatformDeviceResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ApprovePlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Vixly.Shared.Kernel.Errors.Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        if (notAllowed is not null)
        {
            return ApprovePlatformDeviceResponse.Failure(notAllowed);
        }

        PlatformDevice? device = await _licensing.DeviceForAsync(context, request.DeviceId, cancellationToken);

        if (device is null)
        {
            return ApprovePlatformDeviceResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        (PlatformSubscription? subscription, Vixly.Shared.Kernel.Errors.Error? refused) = await _licensing.ReadyForSeatAsync(device.PlatformTenantId, cancellationToken);

        if (refused is not null)
        {
            return ApprovePlatformDeviceResponse.Failure(refused);
        }

        PlatformTenantLicensing seats = await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken);

        try
        {
            _licensing.Record(device, device.Approve(PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow), seats,
                PlatformLicensingAdministration.SeatLimit(subscription), null, "platform.licensing.device_approved", null);
        }
        catch (DomainException exception)
        {
            return ApprovePlatformDeviceResponse.Failure(exception.Error);
        }

        _licensing.Notice(device.PlatformTenantId, PlatformLicensingNoticeKind.DeviceAdded, device.Id);

        return await _licensing.SaveAsync(cancellationToken)
            ? ApprovePlatformDeviceResponse.Success()
            : ApprovePlatformDeviceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
