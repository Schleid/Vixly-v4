using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsCustomerAuthErrors.cs
//
// PURPOSE:
// Error codes of customer sign-up and sign-in (Slice 2).
//
// RESPONSIBILITIES:
// - Name the refusals the portal shows
//
// IMPORTANT NOTES:
// - Sign-in failures never say which part was wrong
// =========================================================
public static class OperationsCustomerAuthErrors
{
    public static Error SignInFailed { get; } = Error.Create("ops.customers.sign_in_failed");

    public static Error NotVerified { get; } = Error.Create("ops.customers.not_verified");

    public static Error SessionRequired { get; } = Error.Create("ops.customers.session_required");

    public static Error EmailInvalid { get; } = Error.Create("ops.customers.email_invalid");

    public static Error EmailTaken { get; } = Error.Create("ops.customers.email_taken");

    public static Error PasswordWrong { get; } = Error.Create("ops.customers.password_wrong");

    public static Error LanguageInvalid { get; } = Error.Create("ops.customers.language_invalid");
}
