using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.WithdrawPlatformDeviceRelease;

// =========================================================
// FILE: WithdrawPlatformDeviceReleaseUseCase.cs
//
// PURPOSE:
// A tenant admin withdraws a release still waiting for Vixly (D-L6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Role and step-up rules from the service token (Slice 9, D-K8)
// =========================================================
public sealed class WithdrawPlatformDeviceReleaseUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public WithdrawPlatformDeviceReleaseUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<WithdrawPlatformDeviceReleaseResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        WithdrawPlatformDeviceReleaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Vixly.Shared.Kernel.Errors.Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        if (notAllowed is not null)
        {
            return WithdrawPlatformDeviceReleaseResponse.Failure(notAllowed);
        }

        PlatformDevice? device = await _licensing.DeviceForAsync(context, request.DeviceId, cancellationToken);

        if (device is null)
        {
            return WithdrawPlatformDeviceReleaseResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        try
        {
            _licensing.Record(device, device.WithdrawRelease(PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow),
                await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), int.MaxValue, null, "platform.licensing.release_withdrawn", null);
        }
        catch (DomainException exception)
        {
            return WithdrawPlatformDeviceReleaseResponse.Failure(exception.Error);
        }

        return await _licensing.SaveAsync(cancellationToken)
            ? WithdrawPlatformDeviceReleaseResponse.Success()
            : WithdrawPlatformDeviceReleaseResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
