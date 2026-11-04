using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;

// =========================================================
// FILE: EnrolPlatformDeviceUseCase.cs
//
// PURPOSE:
// A tenant device asks to join: it waits for a tenant admin, holding no seat (D-L4, D-L5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Operations has checked its own permission and step-up for the acting user
// =========================================================
public sealed class EnrolPlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public EnrolPlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<EnrolPlatformDeviceResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        EnrolPlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status != PlatformTenantStatus.Active)
        {
            return EnrolPlatformDeviceResponse.Failure(PlatformLicensingErrors.TenantNotActive);
        }

        if (await _licensing.Store.CountPendingAsync(tenant.Id, cancellationToken) >= PlatformLicensingLimits.MaxPending)
        {
            return EnrolPlatformDeviceResponse.Failure(PlatformLicensingErrors.TooManyPending);
        }

        bool keyed = !string.IsNullOrWhiteSpace(request.PublicKey);

        if (keyed && !_licensing.Keys.IsValidPublicKey(request.PublicKey))
        {
            return EnrolPlatformDeviceResponse.Failure(PlatformLicensingErrors.PublicKeyInvalid);
        }

        PlatformSecret? secret = keyed ? null : _licensing.NewSecret();
        PlatformTenantLicensing seats = await _licensing.SeatsAsync(tenant.Id, cancellationToken);
        PlatformDevice device;
        PlatformDeviceEvent requested;

        try
        {
            (device, requested) = PlatformDevice.Enrol(tenant.Id, seats.NextNumber(_licensing.UtcNow), request.Name, request.Kind, keyed ? request.PublicKey : null, secret?.Hash,
                PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow, request.Origin);
        }
        catch (DomainException exception)
        {
            return EnrolPlatformDeviceResponse.Failure(exception.Error);
        }

        _licensing.Store.AddDevice(device);
        _licensing.Store.AddEvents(new[] { requested });
        _licensing.Audit(null, "platform.licensing.device_requested", null, "PlatformDevice", device.Id, new { number = device.Number, proof = device.Proof.ToString(), actor = context.ActorName });

        return await _licensing.SaveAsync(cancellationToken)
            ? EnrolPlatformDeviceResponse.Success(new PlatformDeviceEnrolment(device.Id, device.Number, device.Status, secret?.Value))
            : EnrolPlatformDeviceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
