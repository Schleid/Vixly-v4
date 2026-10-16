using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.SendPlatformFirstDeviceLink;

// =========================================================
// FILE: SendPlatformFirstDeviceLinkUseCase.cs
//
// PURPOSE:
// Vixly staff email the owner a first-device link (tenants created by staff, D-L7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SendPlatformFirstDeviceLinkUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public SendPlatformFirstDeviceLinkUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<SendPlatformFirstDeviceLinkResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SendPlatformFirstDeviceLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return SendPlatformFirstDeviceLinkResponse.Failure(refused);
        }

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);
        PlatformTenantRegistration? registration = await _licensing.Registrations.FindByTenantAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return SendPlatformFirstDeviceLinkResponse.Failure(PlatformCommonErrors.NotFound);
        }

        bool registering = registration?.StartFirstDevice(_licensing.UtcNow) == true;

        if ((!registering && tenant.Status != PlatformTenantStatus.Active) || await _licensing.Store.CountHoldingSeatsAsync(tenant.Id, cancellationToken) > 0)
        {
            return SendPlatformFirstDeviceLinkResponse.Failure(PlatformLicensingErrors.RegistrationNotReady);
        }

        if (await _licensing.LinkLimitReachedAsync(tenant.Id, cancellationToken))
        {
            return SendPlatformFirstDeviceLinkResponse.Failure(PlatformLicensingErrors.TooManyLinks);
        }

        _licensing.Store.AddEvents(new[] { PlatformDeviceEvent.Record(tenant.Id, null, PlatformDeviceEventCodes.LinkSent, PlatformDeviceActor.Staff(context.PlatformUserId), _licensing.UtcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["kind"] = nameof(PlatformDeviceLinkKind.FirstDevice) }) });
        _licensing.Notice(tenant.Id, PlatformLicensingNoticeKind.FirstDeviceLink, registrationId: registering ? registration!.Id : null);
        _licensing.Audit(context.PlatformUserId, "platform.licensing.first_device_link_requested", PlatformPermissions.LicensingManage, nameof(PlatformTenant), tenant.Id);

        return await _licensing.SaveAsync(cancellationToken)
            ? SendPlatformFirstDeviceLinkResponse.Success()
            : SendPlatformFirstDeviceLinkResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
