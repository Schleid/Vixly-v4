using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformBillingErrors.cs
//
// PURPOSE:
// Error codes for invoices, payments, credit notes, numbering, tax rates and billing settings.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.billing.*; never reword one that shipped
// =========================================================
public static class PlatformBillingErrors
{
    public static Error InvoiceNotFound { get; } = Error.Create("platform.billing.invoice_not_found");

    public static Error NothingToBill { get; } = Error.Create("platform.billing.nothing_to_bill");

    public static Error LineInvalid { get; } = Error.Create("platform.billing.line_invalid");

    public static Error CurrencyInvalid { get; } = Error.Create("platform.billing.currency_invalid");

    public static Error InvoiceVoid { get; } = Error.Create("platform.billing.invoice_void");

    public static Error InvoicePaid { get; } = Error.Create("platform.billing.invoice_paid");

    public static Error AmountInvalid { get; } = Error.Create("platform.billing.amount_invalid");

    public static Error AmountAboveBalance { get; } = Error.Create("platform.billing.amount_above_balance");

    public static Error ReferenceRequired { get; } = Error.Create("platform.billing.reference_required");

    public static Error ReferenceTooLong { get; } = Error.Create("platform.billing.reference_too_long");

    public static Error NoteTooLong { get; } = Error.Create("platform.billing.note_too_long");

    public static Error ReceivedDateInvalid { get; } = Error.Create("platform.billing.received_date_invalid");

    public static Error MethodNotAllowed { get; } = Error.Create("platform.billing.method_not_allowed");

    public static Error PaymentNotFound { get; } = Error.Create("platform.billing.payment_not_found");

    public static Error PaymentReversed { get; } = Error.Create("platform.billing.payment_reversed");

    public static Error ReasonRequired { get; } = Error.Create("platform.billing.reason_required");

    public static Error ReasonTooLong { get; } = Error.Create("platform.billing.reason_too_long");

    public static Error CreditAboveRemaining { get; } = Error.Create("platform.billing.credit_above_remaining");

    public static Error CreditPending { get; } = Error.Create("platform.billing.credit_pending");

    public static Error CreditNotFound { get; } = Error.Create("platform.billing.credit_not_found");

    public static Error CreditNotPending { get; } = Error.Create("platform.billing.credit_not_pending");

    public static Error OwnRequest { get; } = Error.Create("platform.billing.own_request");

    public static Error VoidAfterPayment { get; } = Error.Create("platform.billing.void_after_payment");

    public static Error ActorRequired { get; } = Error.Create("platform.billing.actor_required");

    public static Error TaxRateInvalid { get; } = Error.Create("platform.billing.tax_rate_invalid");

    public static Error TaxLabelRequired { get; } = Error.Create("platform.billing.tax_label_required");

    public static Error CountryInvalid { get; } = Error.Create("platform.billing.country_invalid");

    public static Error RegionInvalid { get; } = Error.Create("platform.billing.region_invalid");

    public static Error TaxRateNotFound { get; } = Error.Create("platform.billing.tax_rate_not_found");

    public static Error SettingInvalid { get; } = Error.Create("platform.billing.setting_invalid");

    public static Error PrefixInvalid { get; } = Error.Create("platform.billing.prefix_invalid");

    public static Error TextTooLong { get; } = Error.Create("platform.billing.text_too_long");

    public static Error ProfileInvalid { get; } = Error.Create("platform.billing.profile_invalid");

    public static Error LogoInvalid { get; } = Error.Create("platform.billing.logo_invalid", ("maxKilobytes", PlatformBillingLimits.MaxLogoKilobytes.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    public static Error ProfileMissing { get; } = Error.Create("platform.billing.profile_missing");

    public static Error TaxIdTooLong { get; } = Error.Create("platform.billing.tax_id_too_long");

    public static Error SupportContactInvalid { get; } = Error.Create("platform.billing.support_contact_invalid");

    public static Error DocumentNotFound { get; } = Error.Create("platform.billing.document_not_found");

    public static Error SequenceExhausted { get; } = Error.Create("platform.billing.sequence_exhausted");
}
