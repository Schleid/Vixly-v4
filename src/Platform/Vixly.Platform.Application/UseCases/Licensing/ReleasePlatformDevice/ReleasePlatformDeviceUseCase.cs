using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.ReleasePlatformDevice;

// =========================================================
// FILE: ReleasePlatformDeviceUseCase.cs
//
// PURPOSE:
// A tenant admin frees a device's seat: at once within the allowance, otherwise Vixly reviews it; a lost device stops at once (D-L6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Role and step-up rules from the service token (Slice 9, D-K8)
// =========================================================
public sealed class ReleasePlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public ReleasePlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<ReleasePlatformDeviceResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ReleasePlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Vixly.Shared.Kernel.Errors.Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        if (notAllowed is not null)
        {
            return ReleasePlatformDeviceResponse.Failure(notAllowed);
        }

        PlatformDevice? device = await _licensing.DeviceForAsync(context, request.DeviceId, cancellationToken);

        if (device is null)
        {
            return ReleasePlatformDeviceResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        PlatformSubscription? subscription = await _licensing.Subscriptions.Subscriptions.GetCurrentAsync(device.PlatformTenantId, cancellationToken);
        bool within = await _licensing.WithinAllowanceAsync(device.PlatformTenantId, subscription?.Licences ?? 0, cancellationToken);
        PlatformDeviceChange change;

        try
        {
            change = device.Release(request.Reason, within, PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow);
        }
        catch (DomainException exception)
        {
            return ReleasePlatformDeviceResponse.Failure(exception.Error);
        }

        _licensing.Record(device, change, await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), int.MaxValue, null,
            PlatformLicensingAdministration.ActionFor(change.Event.EventCode), null);

        return await _licensing.SaveAsync(cancellationToken)
            ? ReleasePlatformDeviceResponse.Success(device.Status, device.AwaitsReview)
            : ReleasePlatformDeviceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
