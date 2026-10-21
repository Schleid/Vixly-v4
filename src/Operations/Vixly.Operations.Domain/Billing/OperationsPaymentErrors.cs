using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsPaymentErrors.cs
//
// PURPOSE:
// Error codes of payments, cash drawers and refunds (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsPaymentErrors
{
    public static Error NotFound { get; } = Error.Create("ops.payments.not_found");

    public static Error MethodNotAccepted { get; } = Error.Create("ops.payments.method_not_accepted");

    public static Error AmountInvalid { get; } = Error.Create("ops.payments.amount_invalid");

    public static Error NotInFull { get; } = Error.Create("ops.payments.not_in_full");

    public static Error NothingOwed { get; } = Error.Create("ops.payments.nothing_owed");

    public static Error CashGivenTooLow { get; } = Error.Create("ops.payments.cash_given_too_low");

    public static Error OneCashTender { get; } = Error.Create("ops.payments.one_cash_tender");

    public static Error DrawerRequired { get; } = Error.Create("ops.payments.drawer_required");

    public static Error DrawerOpen { get; } = Error.Create("ops.payments.drawer_open");

    public static Error DrawerClosed { get; } = Error.Create("ops.payments.drawer_closed");

    public static Error DrawerNotFound { get; } = Error.Create("ops.payments.drawer_not_found");

    public static Error ZelleTermsRequired { get; } = Error.Create("ops.payments.zelle_terms_required");

    public static Error NotAwaiting { get; } = Error.Create("ops.payments.not_awaiting");

    public static Error RefundTooHigh { get; } = Error.Create("ops.payments.refund_too_high");

    public static Error ReasonRequired { get; } = Error.Create("ops.payments.reason_required");

    public static Error TextInvalid { get; } = Error.Create("ops.payments.text_invalid");

    public static Error ZelleIncomplete { get; } = Error.Create("ops.payments.zelle_incomplete");

    public static Error PictureInvalid { get; } = Error.Create("ops.payments.picture_invalid");

    public static Error MethodNameInvalid { get; } = Error.Create("ops.payments.method_name_invalid");

    public static Error OwnPayment { get; } = Error.Create("ops.payments.own_payment");

    public static Error DrawerShort { get; } = Error.Create("ops.payments.drawer_short");
}
