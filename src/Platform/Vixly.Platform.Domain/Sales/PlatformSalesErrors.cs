using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesErrors.cs
//
// PURPOSE:
// Error codes for Sales settings, promotions and offer links.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.sales_settings.*, platform.promotions.* and platform.offers.*; never reword one that shipped
// =========================================================
public static class PlatformSalesErrors
{
    public static Error SettingInvalid { get; } =
        Error.Create("platform.sales_settings.value_invalid");

    public static Error PromotionCodeInvalid { get; } =
        Error.Create("platform.promotions.code_invalid");

    public static Error PromotionCodeTaken { get; } =
        Error.Create("platform.promotions.code_taken");

    public static Error PromotionNameRequired { get; } =
        Error.Create("platform.promotions.name_required");

    public static Error PromotionNameTooLong { get; } =
        Error.Create("platform.promotions.name_too_long");

    public static Error PromotionDatesInvalid { get; } =
        Error.Create("platform.promotions.dates_invalid");

    public static Error PromotionUseInvalid { get; } =
        Error.Create("platform.promotions.use_invalid");

    public static Error PromotionLimitInvalid { get; } =
        Error.Create("platform.promotions.limit_invalid");

    public static Error PromotionNotEditable { get; } =
        Error.Create("platform.promotions.not_editable");

    public static Error PromotionNotPending { get; } =
        Error.Create("platform.promotions.not_pending");

    public static Error PromotionNotFound { get; } =
        Error.Create("platform.promotions.not_found");

    public static Error PromotionNotActive { get; } =
        Error.Create("platform.promotions.not_active");

    public static Error PromotionNotStarted { get; } =
        Error.Create("platform.promotions.not_started");

    public static Error PromotionExpired { get; } =
        Error.Create("platform.promotions.expired");

    public static Error PromotionNotForPlan { get; } =
        Error.Create("platform.promotions.not_for_plan");

    public static Error PromotionNotForTerm { get; } =
        Error.Create("platform.promotions.not_for_term");

    public static Error PromotionNotAtRegistration { get; } =
        Error.Create("platform.promotions.not_at_registration");

    public static Error PromotionNotByStaff { get; } =
        Error.Create("platform.promotions.not_by_staff");

    public static Error PromotionUsedUp { get; } =
        Error.Create("platform.promotions.used_up");

    public static Error PromotionAlreadyUsed { get; } =
        Error.Create("platform.promotions.already_used");

    public static Error OwnSubmission { get; } =
        Error.Create("platform.sales.own_submission");

    public static Error ReasonRequired { get; } =
        Error.Create("platform.sales.reason_required");

    public static Error ReasonTooLong { get; } =
        Error.Create("platform.sales.reason_too_long");

    public static Error ActorRequired { get; } =
        Error.Create("platform.sales.actor_required");

    public static Error OfferCompanyRequired { get; } =
        Error.Create("platform.offers.company_required");

    public static Error OfferEmailInvalid { get; } =
        Error.Create("platform.offers.email_invalid");

    public static Error OfferTermsInvalid { get; } =
        Error.Create("platform.offers.terms_invalid");

    public static Error OfferExpiryInvalid { get; } =
        Error.Create("platform.offers.expiry_invalid");

    public static Error OfferUsesInvalid { get; } =
        Error.Create("platform.offers.uses_invalid");

    public static Error OfferNotEditable { get; } =
        Error.Create("platform.offers.not_editable");

    public static Error OfferNotPending { get; } =
        Error.Create("platform.offers.not_pending");

    public static Error OfferNotIssued { get; } =
        Error.Create("platform.offers.not_issued");

    public static Error OfferNotFound { get; } =
        Error.Create("platform.offers.not_found");

    public static Error OfferUnavailable { get; } =
        Error.Create("platform.offers.unavailable");

    public static Error OfferEmailMismatch { get; } =
        Error.Create("platform.offers.email_mismatch");

    public static Error OfferNotRevocable { get; } =
        Error.Create("platform.offers.not_revocable");
}
