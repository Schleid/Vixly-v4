using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecoveryForOwner;

// =========================================================
// FILE: StartPlatformOwnerRecoveryForOwnerUseCase.cs
//
// PURPOSE:
// The owner, confirmed by email in Operations, asks for a device recovery link (D-L8).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Role rules from the service token (Slice 9, D-K8)
// - The link goes to the owner contact only; it is never returned
// =========================================================
public sealed class StartPlatformOwnerRecoveryForOwnerUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public StartPlatformOwnerRecoveryForOwnerUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<StartPlatformOwnerRecoveryForOwnerResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        StartPlatformOwnerRecoveryForOwnerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Vixly.Shared.Kernel.Errors.Error? notAllowed = PlatformTenantAccess.Require(context, false, PlatformTenantActorRole.Owner);

        if (notAllowed is not null)
        {
            return StartPlatformOwnerRecoveryForOwnerResponse.Failure(notAllowed);
        }

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status != PlatformTenantStatus.Active)
        {
            return StartPlatformOwnerRecoveryForOwnerResponse.Failure(PlatformLicensingErrors.TenantNotActive);
        }

        if (await _licensing.LinkLimitReachedAsync(tenant.Id, cancellationToken))
        {
            return StartPlatformOwnerRecoveryForOwnerResponse.Failure(PlatformLicensingErrors.TooManyLinks);
        }

        _licensing.Store.AddEvents(new[] { PlatformDeviceEvent.Record(tenant.Id, null, PlatformDeviceEventCodes.LinkSent, PlatformLicensingAdministration.ActorOf(context), _licensing.UtcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["kind"] = nameof(PlatformDeviceLinkKind.OwnerRecovery) }) });
        _licensing.Notice(tenant.Id, PlatformLicensingNoticeKind.RecoveryLink);
        _licensing.Audit(null, "platform.licensing.owner_recovery_started", null, nameof(PlatformTenant), tenant.Id, new { actor = context.ActorName });

        return await _licensing.SaveAsync(cancellationToken)
            ? StartPlatformOwnerRecoveryForOwnerResponse.Success()
            : StartPlatformOwnerRecoveryForOwnerResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
