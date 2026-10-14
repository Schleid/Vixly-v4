using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanErrors.cs
//
// PURPOSE:
// The errors raised by plans and their versions.
//
// RESPONSIBILITIES:
// - Declare every error code of this area in one place
//
// IMPORTANT NOTES:
// - Codes start with platform.plans. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformPlanErrors
{
    public static Error CodeInvalid { get; } =
        Error.Create("platform.plans.code_invalid", ("minLength", PlatformSubscriptionLimits.PlanCodeMinLength), ("maxLength", PlatformSubscriptionLimits.PlanCodeMaxLength));

    public static Error CodeTaken { get; } =
        Error.Create("platform.plans.code_taken");

    public static Error NameRequired { get; } =
        Error.Create("platform.plans.name_required");

    public static Error NameTooLong { get; } =
        Error.Create("platform.plans.name_too_long", ("maxLength", PlatformSubscriptionLimits.PlanNameMaxLength));

    public static Error DescriptionTooLong { get; } =
        Error.Create("platform.plans.description_too_long", ("maxLength", PlatformSubscriptionLimits.PlanDescriptionMaxLength));

    public static Error EnglishNameRequired { get; } =
        Error.Create("platform.plans.english_name_required");

    public static Error CurrencyInvalid { get; } =
        Error.Create("platform.plans.currency_invalid");

    public static Error CurrencyFixed { get; } =
        Error.Create("platform.plans.currency_fixed");

    public static Error PriceInvalid { get; } =
        Error.Create("platform.plans.price_invalid");

    public static Error PriceRequired { get; } =
        Error.Create("platform.plans.price_required");

    public static Error MinimumLicencesInvalid { get; } =
        Error.Create("platform.plans.minimum_licences_invalid", ("maxCount", PlatformSubscriptionLimits.MaxLicences));

    public static Error TrialDaysInvalid { get; } =
        Error.Create("platform.plans.trial_days_invalid", ("maxCount", PlatformSubscriptionLimits.MaxTrialDays));

    public static Error LimitInvalid { get; } =
        Error.Create("platform.plans.limit_invalid", ("maxCount", PlatformSubscriptionLimits.MaxLimitValue));

    public static Error MinimumAboveDeviceLimit { get; } =
        Error.Create("platform.plans.minimum_above_device_limit");

    public static Error FeatureInvalid { get; } =
        Error.Create("platform.plans.feature_invalid");

    public static Error DisplayOrderInvalid { get; } =
        Error.Create("platform.plans.display_order_invalid", ("maxCount", PlatformSubscriptionLimits.MaxDisplayOrder));

    public static Error NoDraft { get; } =
        Error.Create("platform.plans.no_draft");

    public static Error DraftExists { get; } =
        Error.Create("platform.plans.draft_exists");

    public static Error NothingPublished { get; } =
        Error.Create("platform.plans.nothing_published");

    public static Error AlreadyRetired { get; } =
        Error.Create("platform.plans.already_retired");

    public static Error NotRetired { get; } =
        Error.Create("platform.plans.not_retired");

    public static Error Retired { get; } =
        Error.Create("platform.plans.retired");

    public static Error VersionNotFound { get; } =
        Error.Create("platform.plans.version_not_found");

    public static Error ActorRequired { get; } =
        Error.Create("platform.plans.actor_required");

    public static Error DraftSubmitted { get; } =
        Error.Create("platform.plans.draft_submitted");

    public static Error DraftNotSubmitted { get; } =
        Error.Create("platform.plans.draft_not_submitted");

    public static Error OwnSubmission { get; } =
        Error.Create("platform.plans.own_submission");

    public static Error RejectionReasonRequired { get; } =
        Error.Create("platform.plans.rejection_reason_required");
}
