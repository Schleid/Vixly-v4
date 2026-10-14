using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSubscriptionTenantStatusHandler.cs
//
// PURPOSE:
// Keeps the subscription in step with the tenant status (D-S10, Slice 2 D-T6).
//
// RESPONSIBILITIES:
// - Suspend the subscription when the tenant is suspended, restore it on reactivation
// - End it when the tenant is deactivated or banned
//
// IMPORTANT NOTES:
// - Second handler of the tenant status message, beside the notice handler
// - Idempotent: a repeated message changes nothing; the dispatcher saves with the message
// =========================================================
public sealed class PlatformSubscriptionTenantStatusHandler : IPlatformOutboxHandler
{
    private readonly PlatformSubscriptionAdministration _administration;

    public PlatformSubscriptionTenantStatusHandler(PlatformSubscriptionAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public string MessageType => PlatformMessageTypes.TenantStatusChanged;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformTenantStatusChanged change = PlatformOutboxWriter.Read<PlatformTenantStatusChanged>(message);
        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(change.PlatformTenantId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);

        if (subscription is null || plan is null)
        {
            return;
        }

        DateTimeOffset utcNow = _administration.UtcNow;

        switch (change.ToStatus)
        {
            case PlatformTenantStatus.Suspended when subscription.SuspendWithTenant(utcNow) is { } suspended:
                _administration.RecordChange(null, subscription, plan, suspended, null, "platform.subscriptions.suspended_with_tenant");
                break;
            case PlatformTenantStatus.Active when subscription.RestoreWithTenant(utcNow) is { } restored:
                _administration.RecordChange(null, subscription, plan, restored, null, "platform.subscriptions.restored_with_tenant");
                break;
            case PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned when subscription.EndWithTenant(utcNow) is { } ended:
                _administration.RecordChange(null, subscription, plan, ended, null, "platform.subscriptions.ended_with_tenant");
                break;
        }
    }
}
