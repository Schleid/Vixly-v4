using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.RevokePlatformDevice;

// =========================================================
// FILE: RevokePlatformDeviceUseCase.cs
//
// PURPOSE:
// Vixly staff revoke a device for security; its seat is freed (D-L13).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RevokePlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public RevokePlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<RevokePlatformDeviceResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RevokePlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RevokePlatformDeviceResponse.Failure(refused);
        }

        PlatformDevice? device = await _licensing.Store.GetDeviceAsync(request.DeviceId, cancellationToken);

        if (device is null)
        {
            return RevokePlatformDeviceResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length > 500)
        {
            return RevokePlatformDeviceResponse.Failure(PlatformLicensingErrors.ReasonRequired);
        }

        PlatformDeviceChange change;

        try
        {
            change = device.Revoke(PlatformDeviceReason.Security, PlatformDeviceActor.Staff(context.PlatformUserId), _licensing.UtcNow, request.Note.Trim());
        }
        catch (DomainException exception)
        {
            return RevokePlatformDeviceResponse.Failure(exception.Error);
        }

        _licensing.Record(device, change, await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), int.MaxValue, context.PlatformUserId,
            "platform.licensing.device_revoked", PlatformPermissions.LicensingManage);

        return await _licensing.SaveAsync(cancellationToken)
            ? RevokePlatformDeviceResponse.Success()
            : RevokePlatformDeviceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
