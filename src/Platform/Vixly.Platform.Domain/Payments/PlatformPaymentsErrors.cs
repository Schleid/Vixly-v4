using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformPaymentsErrors.cs
//
// PURPOSE:
// Error codes for card payments: merchant account, saved cards, charges, refunds and payment links.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.payments.*; never reword one that shipped
// - Provider error text is never shown; only these codes (S-9)
// =========================================================
public static class PlatformPaymentsErrors
{
    public static Error NotReady { get; } = Error.Create("platform.payments.not_ready");

    public static Error CurrencyNotAccepted { get; } = Error.Create("platform.payments.currency_not_accepted");

    public static Error PublishableKeyInvalid { get; } = Error.Create("platform.payments.publishable_key_invalid");

    public static Error CurrencyInvalid { get; } = Error.Create("platform.payments.currency_invalid");

    public static Error TooManyCurrencies { get; } = Error.Create("platform.payments.too_many_currencies", ("max", PlatformPaymentsLimits.MaxCurrencies.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    public static Error ModeMismatch { get; } = Error.Create("platform.payments.mode_mismatch");

    public static Error SecretMissing { get; } = Error.Create("platform.payments.secret_missing");

    public static Error ProviderUnavailable { get; } = Error.Create("platform.payments.provider_unavailable");

    public static Error ProviderRefused { get; } = Error.Create("platform.payments.provider_refused");

    public static Error ProviderReferenceInvalid { get; } = Error.Create("platform.payments.provider_reference_invalid");

    public static Error CardNotFound { get; } = Error.Create("platform.payments.card_not_found");

    public static Error NoSavedCard { get; } = Error.Create("platform.payments.no_saved_card");

    public static Error CardExpired { get; } = Error.Create("platform.payments.card_expired");

    public static Error CardRemoved { get; } = Error.Create("platform.payments.card_removed");

    public static Error CardInvalid { get; } = Error.Create("platform.payments.card_invalid");

    public static Error ChargeOpen { get; } = Error.Create("platform.payments.charge_open");

    public static Error NothingToCharge { get; } = Error.Create("platform.payments.nothing_to_charge");

    public static Error AmountInvalid { get; } = Error.Create("platform.payments.amount_invalid");

    public static Error LinkInvalid { get; } = Error.Create("platform.payments.link_invalid");

    public static Error RefundNotAllowed { get; } = Error.Create("platform.payments.refund_not_allowed");

    public static Error RefundAboveCharged { get; } = Error.Create("platform.payments.refund_above_charged");

    public static Error TransactionNotFound { get; } = Error.Create("platform.payments.transaction_not_found");

    public static Error TransactionClosed { get; } = Error.Create("platform.payments.transaction_closed");

    public static Error ConsentInvalid { get; } = Error.Create("platform.payments.consent_invalid");

    public static Error AutoChargeOff { get; } = Error.Create("platform.payments.auto_charge_off");

    public static Error EventInvalid { get; } = Error.Create("platform.payments.event_invalid");

    public static Error ActorRequired { get; } = Error.Create("platform.payments.actor_required");
}
