using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.RequestPlatformFirstDeviceLink;

// =========================================================
// FILE: RequestPlatformFirstDeviceLinkUseCase.cs
//
// PURPOSE:
// After account setup, Operations asks for the owner's first-device link (D-L7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Role rules from the service token (Slice 9, D-K8)
// - At most 3 links per hour per tenant
// =========================================================
public sealed class RequestPlatformFirstDeviceLinkUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public RequestPlatformFirstDeviceLinkUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<RequestPlatformFirstDeviceLinkResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        RequestPlatformFirstDeviceLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Vixly.Shared.Kernel.Errors.Error? notAllowed = PlatformTenantAccess.Require(context, false, PlatformTenantActorRole.Owner);

        if (notAllowed is not null)
        {
            return RequestPlatformFirstDeviceLinkResponse.Failure(notAllowed);
        }

        PlatformTenantRegistration? registration = await _licensing.Registrations.FindByTenantAsync(context.PlatformTenantId, cancellationToken);

        if (registration is null || !registration.StartFirstDevice(_licensing.UtcNow) || await _licensing.Store.CountHoldingSeatsAsync(context.PlatformTenantId, cancellationToken) > 0)
        {
            return RequestPlatformFirstDeviceLinkResponse.Failure(PlatformLicensingErrors.RegistrationNotReady);
        }

        if (await _licensing.LinkLimitReachedAsync(context.PlatformTenantId, cancellationToken))
        {
            return RequestPlatformFirstDeviceLinkResponse.Failure(PlatformLicensingErrors.TooManyLinks);
        }

        _licensing.Store.AddEvents(new[] { PlatformDeviceEvent.Record(context.PlatformTenantId, null, PlatformDeviceEventCodes.LinkSent, PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["kind"] = nameof(PlatformDeviceLinkKind.FirstDevice) }) });
        _licensing.Notice(context.PlatformTenantId, PlatformLicensingNoticeKind.FirstDeviceLink, registrationId: registration.Id);
        _licensing.Audit(null, "platform.licensing.first_device_link_requested", null, nameof(PlatformTenant), context.PlatformTenantId);

        return await _licensing.SaveAsync(cancellationToken)
            ? RequestPlatformFirstDeviceLinkResponse.Success()
            : RequestPlatformFirstDeviceLinkResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
