using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionErrors.cs
//
// PURPOSE:
// The errors raised by subscriptions and limit overrides.
//
// RESPONSIBILITIES:
// - Declare every error code of this area in one place
//
// IMPORTANT NOTES:
// - Codes start with platform.subscriptions. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformSubscriptionErrors
{
    public static Error TenantRequired { get; } =
        Error.Create("platform.subscriptions.tenant_required");

    public static Error ActorRequired { get; } =
        Error.Create("platform.subscriptions.actor_required");

    public static Error AlreadySubscribed { get; } =
        Error.Create("platform.subscriptions.already_subscribed");

    public static Error NoSubscription { get; } =
        Error.Create("platform.subscriptions.no_subscription");

    public static Error TenantCannotSubscribe { get; } =
        Error.Create("platform.subscriptions.tenant_cannot_subscribe");

    public static Error PlanNotAvailable { get; } =
        Error.Create("platform.subscriptions.plan_not_available");

    public static Error TermNotOffered { get; } =
        Error.Create("platform.subscriptions.term_not_offered");

    public static Error TermInvalid { get; } =
        Error.Create("platform.subscriptions.term_invalid");

    public static Error TrialNotOffered { get; } =
        Error.Create("platform.subscriptions.trial_not_offered");

    public static Error BelowMinimum(int minimum)
    {
        return Error.Create("platform.subscriptions.below_minimum", ("minimum", minimum));
    }

    public static Error AboveLimit(int maximum)
    {
        return Error.Create("platform.subscriptions.above_limit", ("maximum", maximum));
    }

    public static Error BelowActiveLicences(int active)
    {
        return Error.Create("platform.subscriptions.below_active_licences", ("active", active));
    }

    public static Error NothingToChange { get; } =
        Error.Create("platform.subscriptions.nothing_to_change");

    public static Error NoScheduledChange { get; } =
        Error.Create("platform.subscriptions.no_scheduled_change");

    public static Error NotTrialing { get; } =
        Error.Create("platform.subscriptions.not_trialing");

    public static Error TrialEndInvalid { get; } =
        Error.Create("platform.subscriptions.trial_end_invalid");

    public static Error NotAwaitingPayment { get; } =
        Error.Create("platform.subscriptions.not_awaiting_payment");

    public static Error CancellationAlreadyRequested { get; } =
        Error.Create("platform.subscriptions.cancellation_already_requested");

    public static Error NoCancellationRequested { get; } =
        Error.Create("platform.subscriptions.no_cancellation_requested");

    public static Error Ended { get; } =
        Error.Create("platform.subscriptions.ended");

    public static Error NotAllowedInStatus(PlatformSubscriptionStatus status)
    {
        return Error.Create("platform.subscriptions.not_allowed_in_status", ("status", status.ToString()));
    }

    public static Error ReasonRequired { get; } =
        Error.Create("platform.subscriptions.reason_required");

    public static Error ReasonTooLong { get; } =
        Error.Create("platform.subscriptions.reason_too_long", ("maxLength", PlatformSubscriptionLimits.ReasonMaxLength));

    public static Error LicencesInvalid { get; } =
        Error.Create("platform.subscriptions.licences_invalid", ("maxCount", PlatformSubscriptionLimits.MaxLicences));

    public static Error OverrideValueInvalid { get; } =
        Error.Create("platform.subscriptions.override_value_invalid", ("maxCount", PlatformSubscriptionLimits.MaxLimitValue));

    public static Error OverrideEndInvalid { get; } =
        Error.Create("platform.subscriptions.override_end_invalid");

    public static Error OverrideLimitInvalid { get; } =
        Error.Create("platform.subscriptions.override_limit_invalid");

    public static Error OverrideClosed { get; } =
        Error.Create("platform.subscriptions.override_closed");
}
