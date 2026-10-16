using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.DenyPlatformDevice;

// =========================================================
// FILE: DenyPlatformDeviceUseCase.cs
//
// PURPOSE:
// A tenant admin refuses a waiting device (D-L4).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Role and step-up rules from the service token (Slice 9, D-K8)
// =========================================================
public sealed class DenyPlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public DenyPlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<DenyPlatformDeviceResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        DenyPlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Vixly.Shared.Kernel.Errors.Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        if (notAllowed is not null)
        {
            return DenyPlatformDeviceResponse.Failure(notAllowed);
        }

        PlatformDevice? device = await _licensing.DeviceForAsync(context, request.DeviceId, cancellationToken);

        if (device is null)
        {
            return DenyPlatformDeviceResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        try
        {
            _licensing.Record(device, device.Deny(PlatformDeviceReason.NotRecognized, PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow),
                await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), 0, null, "platform.licensing.device_denied", null);
        }
        catch (DomainException exception)
        {
            return DenyPlatformDeviceResponse.Failure(exception.Error);
        }

        return await _licensing.SaveAsync(cancellationToken)
            ? DenyPlatformDeviceResponse.Success()
            : DenyPlatformDeviceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
