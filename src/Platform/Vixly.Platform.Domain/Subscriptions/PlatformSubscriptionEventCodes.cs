namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionEventCodes.cs
//
// PURPOSE:
// The codes of a subscription's history events (D-4).
//
// RESPONSIBILITIES:
// - Name every event the subscription aggregate records
//
// IMPORTANT NOTES:
// - History is code plus arguments, so it reads in the reader's language
// - Each code needs en, fr and es text under ui.subscription_history.
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformSubscriptionEventCodes
{
    public const string StartedTrial = "platform.subscription_history.started_trial";

    public const string StartedAwaitingPayment = "platform.subscription_history.started_awaiting_payment";

    public const string Activated = "platform.subscription_history.activated";

    public const string OfflinePaymentRecorded = "platform.subscription_history.offline_payment_recorded";

    public const string TrialExtended = "platform.subscription_history.trial_extended";

    public const string TrialEnded = "platform.subscription_history.trial_ended";

    public const string TrialExpired = "platform.subscription_history.trial_expired";

    public const string Changed = "platform.subscription_history.changed";

    public const string ChangeScheduled = "platform.subscription_history.change_scheduled";

    public const string ScheduledChangeCancelled = "platform.subscription_history.scheduled_change_cancelled";

    public const string Renewed = "platform.subscription_history.renewed";

    public const string CancellationRequested = "platform.subscription_history.cancellation_requested";

    public const string Resumed = "platform.subscription_history.resumed";

    public const string Ended = "platform.subscription_history.ended";

    public const string SuspendedWithTenant = "platform.subscription_history.suspended_with_tenant";

    public const string RestoredWithTenant = "platform.subscription_history.restored_with_tenant";
    public const string ActivatedNothingDue = "platform.subscription_history.activated_nothing_due";
    public const string DiscountRequested = "platform.subscription_history.discount_requested";
    public const string DiscountApplied = "platform.subscription_history.discount_applied";
    public const string DiscountApproved = "platform.subscription_history.discount_approved";
    public const string DiscountRejected = "platform.subscription_history.discount_rejected";
    public const string DiscountEnded = "platform.subscription_history.discount_ended";
    public const string ChargeAdded = "platform.subscription_history.charge_added";
    public const string ChargeCancelled = "platform.subscription_history.charge_cancelled";
    public const string SetupFeeChanged = "platform.subscription_history.setup_fee_changed";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        StartedTrial, StartedAwaitingPayment, Activated, OfflinePaymentRecorded, TrialExtended, TrialEnded, TrialExpired,
        Changed, ChangeScheduled, ScheduledChangeCancelled, Renewed, CancellationRequested, Resumed, Ended,
        SuspendedWithTenant, RestoredWithTenant, ActivatedNothingDue, DiscountRequested, DiscountApplied, DiscountApproved,
        DiscountRejected, DiscountEnded, ChargeAdded, ChargeCancelled, SetupFeeChanged
    };
}
