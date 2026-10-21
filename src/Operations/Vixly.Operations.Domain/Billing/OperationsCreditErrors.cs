using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditErrors.cs
//
// PURPOSE:
// Error codes of credit accounts and account charges (Slice 5F step 2b).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsCreditErrors
{
    public static Error NotFound { get; } = Error.Create("ops.credit.not_found");

    public static Error AccountExists { get; } = Error.Create("ops.credit.account_exists");

    public static Error LimitInvalid { get; } = Error.Create("ops.credit.limit_invalid");

    public static Error TermsInvalid { get; } = Error.Create("ops.credit.terms_invalid");

    public static Error ReasonRequired { get; } = Error.Create("ops.credit.reason_required");

    public static Error BalanceOpen { get; } = Error.Create("ops.credit.balance_open");

    public static Error NotActive { get; } = Error.Create("ops.credit.not_active");

    public static Error OverLimit { get; } = Error.Create("ops.credit.over_limit");

    public static Error CreditOff { get; } = Error.Create("ops.credit.credit_off");

    public static Error AlreadyOnAccount { get; } = Error.Create("ops.credit.already_on_account");

    public static Error NothingOwed { get; } = Error.Create("ops.credit.nothing_owed");

    public static Error NotAwaiting { get; } = Error.Create("ops.credit.not_awaiting");

    public static Error OwnRequest { get; } = Error.Create("ops.credit.own_request");

    public static Error SettingsInvalid { get; } = Error.Create("ops.credit.settings_invalid");
}
