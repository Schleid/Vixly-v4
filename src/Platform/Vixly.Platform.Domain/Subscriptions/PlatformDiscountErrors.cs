using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountErrors.cs
//
// PURPOSE:
// Error codes for discounts, one-time charges and the setup fee override.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.discounts.*; never reword one that shipped
// =========================================================
public static class PlatformDiscountErrors
{
    public static Error KindInvalid { get; } =
        Error.Create("platform.discounts.kind_invalid");

    public static Error PercentInvalid { get; } =
        Error.Create("platform.discounts.percent_invalid");

    public static Error AmountInvalid { get; } =
        Error.Create("platform.discounts.amount_invalid");

    public static Error DurationInvalid { get; } =
        Error.Create("platform.discounts.duration_invalid");

    public static Error PeriodsInvalid { get; } =
        Error.Create("platform.discounts.periods_invalid", ("max", PlatformDiscountTerms.MaxPeriods.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    public static Error EndDateInvalid { get; } =
        Error.Create("platform.discounts.end_date_invalid");

    public static Error AlreadyPending { get; } =
        Error.Create("platform.discounts.already_pending");

    public static Error NotPending { get; } =
        Error.Create("platform.discounts.not_pending");

    public static Error NoActiveDiscount { get; } =
        Error.Create("platform.discounts.no_active_discount");

    public static Error OwnRequest { get; } =
        Error.Create("platform.discounts.own_request");

    public static Error DiscountNotFound { get; } =
        Error.Create("platform.discounts.not_found");

    public static Error AlreadyDiscounted { get; } =
        Error.Create("platform.discounts.already_discounted");

    public static Error ChargeDescriptionRequired { get; } =
        Error.Create("platform.discounts.charge_description_required");

    public static Error ChargeDescriptionTooLong { get; } =
        Error.Create("platform.discounts.charge_description_too_long", ("maxLength", PlatformDiscountTerms.ChargeDescriptionMaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    public static Error ChargeNotFound { get; } =
        Error.Create("platform.discounts.charge_not_found");

    public static Error ChargeNotPending { get; } =
        Error.Create("platform.discounts.charge_not_pending");

    public static Error SetupFeeCharged { get; } =
        Error.Create("platform.discounts.setup_fee_charged");
}
