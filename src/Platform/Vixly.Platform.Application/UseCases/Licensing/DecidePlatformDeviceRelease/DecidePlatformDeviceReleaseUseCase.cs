using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.DecidePlatformDeviceRelease;

// =========================================================
// FILE: DecidePlatformDeviceReleaseUseCase.cs
//
// PURPOSE:
// Vixly staff approve or refuse a release over the tenant's allowance (D-L6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class DecidePlatformDeviceReleaseUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public DecidePlatformDeviceReleaseUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<DecidePlatformDeviceReleaseResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DecidePlatformDeviceReleaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingApproveRelease, requireStepUp: true, cancellationToken) is { } refused)
        {
            return DecidePlatformDeviceReleaseResponse.Failure(refused);
        }

        PlatformDevice? device = await _licensing.Store.GetDeviceAsync(request.DeviceId, cancellationToken);

        if (device is null)
        {
            return DecidePlatformDeviceReleaseResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        PlatformDeviceChange change;

        try
        {
            change = device.DecideRelease(request.Approve, request.Note, PlatformDeviceActor.Staff(context.PlatformUserId), _licensing.UtcNow);
        }
        catch (DomainException exception)
        {
            return DecidePlatformDeviceReleaseResponse.Failure(exception.Error);
        }

        _licensing.Record(device, change, await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), int.MaxValue, context.PlatformUserId,
            "platform.licensing.release_decided", PlatformPermissions.LicensingApproveRelease);
        _licensing.Notice(device.PlatformTenantId, PlatformLicensingNoticeKind.ReleaseDecided, device.Id, approved: request.Approve);

        return await _licensing.SaveAsync(cancellationToken)
            ? DecidePlatformDeviceReleaseResponse.Success()
            : DecidePlatformDeviceReleaseResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
