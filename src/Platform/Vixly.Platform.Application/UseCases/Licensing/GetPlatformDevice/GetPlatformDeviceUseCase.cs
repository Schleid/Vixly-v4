using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformDevice;

// =========================================================
// FILE: GetPlatformDeviceUseCase.cs
//
// PURPOSE:
// Reads a device with its history and the actions the reader may take.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public GetPlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<GetPlatformDeviceResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformDeviceResponse.Failure(refused);
        }

        PlatformDeviceItem? device = await _licensing.Queries.GetDeviceAsync(request.DeviceId, _licensing.UtcNow, cancellationToken);

        if (device is null)
        {
            return GetPlatformDeviceResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        return GetPlatformDeviceResponse.Success(new PlatformDeviceDetail(
            device,
            await _licensing.Queries.ListEventsAsync(device.DeviceId, cancellationToken),
            new PlatformLicensingAbilities(
                await _licensing.HoldsAsync(context, PlatformPermissions.LicensingManage, cancellationToken),
                await _licensing.HoldsAsync(context, PlatformPermissions.LicensingApproveRelease, cancellationToken))));
    }
}
