using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardErrors.cs
//
// PURPOSE:
// Error codes of card payments through the company's Stripe account (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Name every refusal of the card desk, the readers and the provider
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText (en, fr, es)
// =========================================================
public static class OperationsCardErrors
{
    public static Error KeysInvalid { get; } = Error.Create("ops.cards.keys_invalid");

    public static Error NotConnected { get; } = Error.Create("ops.cards.not_connected");

    public static Error ProviderRefused { get; } = Error.Create("ops.cards.provider_refused");

    public static Error ProviderUnavailable { get; } = Error.Create("ops.cards.provider_unavailable");

    public static Error ReaderInvalid { get; } = Error.Create("ops.cards.reader_invalid");

    public static Error ChargeNotFound { get; } = Error.Create("ops.cards.charge_not_found");

    public static Error NotPaidYet { get; } = Error.Create("ops.cards.not_paid_yet");

    public static Error AlreadyStarted { get; } = Error.Create("ops.cards.already_started");

    // Review: a payment started on the account could not be closed at Stripe; the account stays as it is.
    public static Error PaymentsOpen { get; } = Error.Create("ops.cards.payments_open");

    public static Error RefundNotPossible { get; } = Error.Create("ops.cards.refund_not_possible");
}
