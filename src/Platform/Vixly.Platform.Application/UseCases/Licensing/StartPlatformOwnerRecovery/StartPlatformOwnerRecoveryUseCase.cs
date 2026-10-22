using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecovery;

// =========================================================
// FILE: StartPlatformOwnerRecoveryUseCase.cs
//
// PURPOSE:
// Vixly staff email the owner a device recovery link (D-L8).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class StartPlatformOwnerRecoveryUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public StartPlatformOwnerRecoveryUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<StartPlatformOwnerRecoveryResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        StartPlatformOwnerRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return StartPlatformOwnerRecoveryResponse.Failure(refused);
        }

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status != PlatformTenantStatus.Active)
        {
            return StartPlatformOwnerRecoveryResponse.Failure(PlatformLicensingErrors.TenantNotActive);
        }

        if (await _licensing.LinkLimitReachedAsync(tenant.Id, cancellationToken))
        {
            return StartPlatformOwnerRecoveryResponse.Failure(PlatformLicensingErrors.TooManyLinks);
        }

        if (request.ReplaceDeviceId is Guid replaceId
            && (await _licensing.Store.GetDeviceAsync(replaceId, cancellationToken) is not { } replaced || replaced.PlatformTenantId != tenant.Id || !replaced.HoldsSeat))
        {
            return StartPlatformOwnerRecoveryResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        PlatformOwnerSupportRequest support;

        try
        {
            support = PlatformOwnerSupportRequest.StartDeviceRecovery(tenant.Id, request.Reason, request.IdentityCheck, request.ReplaceDeviceId, context.PlatformUserId, _licensing.UtcNow);
        }
        catch (DomainException exception)
        {
            return StartPlatformOwnerRecoveryResponse.Failure(exception.Error);
        }

        _licensing.Store.AddSupportRequest(support);
        _licensing.Store.AddEvents(new[] { PlatformDeviceEvent.Record(tenant.Id, null, PlatformDeviceEventCodes.LinkSent, PlatformDeviceActor.Staff(context.PlatformUserId), _licensing.UtcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["kind"] = nameof(PlatformDeviceLinkKind.OwnerRecovery) }) });
        _licensing.Notice(tenant.Id, PlatformLicensingNoticeKind.RecoveryLink, supportRequestId: support.Id, supportKind: PlatformOwnerSupportKind.DeviceRecovery);
        _licensing.Notice(tenant.Id, PlatformLicensingNoticeKind.OwnerSupportStarted, supportRequestId: support.Id, supportKind: PlatformOwnerSupportKind.DeviceRecovery);
        _licensing.Audit(context.PlatformUserId, "platform.licensing.owner_recovery_started", PlatformPermissions.LicensingManage, nameof(PlatformTenant), tenant.Id,
            new { request = support.Id, identityCheck = support.IdentityCheck, replaceDevice = support.ReplaceDeviceId }, support.Reason);

        return await _licensing.SaveAsync(cancellationToken)
            ? StartPlatformOwnerRecoveryResponse.Success()
            : StartPlatformOwnerRecoveryResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
