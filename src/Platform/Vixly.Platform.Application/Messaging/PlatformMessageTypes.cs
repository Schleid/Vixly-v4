namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformMessageTypes.cs
//
// PURPOSE:
// The versioned types of outbox messages (D-T17).
//
// RESPONSIBILITIES:
// - Name every message the Platform writes to its outbox
//
// IMPORTANT NOTES:
// - A type never changes meaning; a new shape is a new version
// =========================================================
public static class PlatformMessageTypes
{
    public const string TenantStatusChanged = "platform.tenant.status_changed.v1";

    public const string TenantSlugCorrected = "platform.tenant.slug_corrected.v1";

    public const string SubscriptionStarted = "platform.subscription.started.v1";

    public const string SubscriptionChanged = "platform.subscription.changed.v1";

    public const string SubscriptionChangeScheduled = "platform.subscription.change_scheduled.v1";

    public const string SubscriptionRenewed = "platform.subscription.renewed.v1";

    public const string SubscriptionStatusChanged = "platform.subscription.status_changed.v1";

    public const string SubscriptionCancellationRequested = "platform.subscription.cancellation_requested.v1";

    public const string SubscriptionTrialExtended = "platform.subscription.trial_extended.v1";

    public const string SubscriptionEnded = "platform.subscription.ended.v1";

    public const string PlanVersionPublished = "platform.plan.version_published.v1";
    public const string SubscriptionDiscountChanged = "platform.subscription.discount_changed.v1";
    public const string SubscriptionChargeChanged = "platform.subscription.charge_changed.v1";
    public const string ApprovalRequested = "platform.sales.approval_requested.v1";
    public const string DiscountEnding = "platform.subscription.discount_ending.v1";
}
