using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantDeviceSeats;

// =========================================================
// FILE: GetTenantDeviceSeatsUseCase.cs
//
// PURPOSE:
// The company's licences for its Licences tab in Operations (owner, 2026-10-21): bought, in use, and a decrease scheduled for the renewal.
//
// RESPONSIBILITIES:
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role, like the device list
// =========================================================
public sealed class GetTenantDeviceSeatsUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public GetTenantDeviceSeatsUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<GetTenantDeviceSeatsResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetTenantDeviceSeatsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscription? subscription = await _licensing.Subscriptions.Subscriptions.GetCurrentAsync(context.PlatformTenantId, cancellationToken);
        int bought = subscription is null || subscription.Status == PlatformSubscriptionStatus.Ended ? 0 : subscription.Licences;
        int inUse = await _licensing.Store.CountHoldingSeatsAsync(context.PlatformTenantId, cancellationToken);
        PlatformSubscriptionScheduledChange? change = subscription?.ScheduledChange is { } scheduled && scheduled.TargetLicences != bought ? scheduled : null;

        return GetTenantDeviceSeatsResponse.Success(bought, inUse, change?.TargetLicences, change?.EffectiveUtc);
    }
}
