using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsChargeErrors.cs
//
// PURPOSE:
// Error codes of rate cards, extras, insurance, the engine and quotes (Slice 4).
//
// RESPONSIBILITIES:
// - Name every refusal about prices
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsChargeErrors
{
    public static Error ServiceUnknown { get; } = Error.Create("ops.charges.service_unknown");

    public static Error CountryInvalid { get; } = Error.Create("ops.charges.country_invalid");

    public static Error CustomerNotFound { get; } = Error.Create("ops.charges.customer_not_found");

    public static Error PriceInvalid { get; } = Error.Create("ops.charges.price_invalid");

    public static Error BandsInvalid { get; } = Error.Create("ops.charges.bands_invalid");

    public static Error ItemsInvalid { get; } = Error.Create("ops.charges.items_invalid");

    public static Error CardDuplicate { get; } = Error.Create("ops.charges.card_duplicate");

    public static Error CardNotFound { get; } = Error.Create("ops.charges.card_not_found");

    public static Error TooMany { get; } = Error.Create("ops.charges.too_many");

    public static Error NameRequired { get; } = Error.Create("ops.charges.name_required");

    public static Error TextInvalid { get; } = Error.Create("ops.charges.text_invalid");

    public static Error ExtraNotFound { get; } = Error.Create("ops.charges.extra_not_found");

    public static Error ExtraDuplicate { get; } = Error.Create("ops.charges.extra_duplicate");

    public static Error PercentInvalid { get; } = Error.Create("ops.charges.percent_invalid");

    public static Error WordsRequired { get; } = Error.Create("ops.charges.words_required");

    public static Error ChoiceInvalid { get; } = Error.Create("ops.charges.choice_invalid");

    public static Error InsuranceInvalid { get; } = Error.Create("ops.charges.insurance_invalid");

    public static Error PiecesInvalid { get; } = Error.Create("ops.charges.pieces_invalid");

    public static Error NotPriced { get; } = Error.Create("ops.charges.not_priced");

    public static Error QuoteNotFound { get; } = Error.Create("ops.charges.quote_not_found");

    public static Error ReasonRequired { get; } = Error.Create("ops.charges.reason_required");

    public static Error PreferredInvalid { get; } = Error.Create("ops.charges.preferred_invalid");

    public static Error DaysInvalid { get; } = Error.Create("ops.charges.days_invalid");

    public static Error InsuranceRefused { get; } = Error.Create("ops.charges.insurance_refused");

    public static Error VersionStarted { get; } = Error.Create("ops.charges.version_started");

    public static Error VersionNotFound { get; } = Error.Create("ops.charges.version_not_found");

    public static Error EffectiveInvalid { get; } = Error.Create("ops.charges.effective_invalid");

    public static Error ItemTypeNotFound { get; } = Error.Create("ops.charges.item_type_not_found");

    public static Error ItemTypeDuplicate { get; } = Error.Create("ops.charges.item_type_duplicate");

    public static Error RouteInvalid { get; } = Error.Create("ops.charges.route_invalid");

    public static Error UnitLocked { get; } = Error.Create("ops.charges.unit_locked");
}
