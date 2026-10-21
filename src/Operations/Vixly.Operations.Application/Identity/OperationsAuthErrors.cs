using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsAuthErrors.cs
//
// PURPOSE:
// Error codes of staff sign-in (Slice 1).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code
//
// IMPORTANT NOTES:
// - Sign-in failures never say whether the email exists
// =========================================================
public static class OperationsAuthErrors
{
    public static Error SignInFailed { get; } = Error.Create("ops.auth.sign_in_failed");

    public static Error CodeInvalid { get; } = Error.Create("ops.auth.code_invalid");

    public static Error SignInExpired { get; } = Error.Create("ops.auth.sign_in_expired");

    public static Error LinkInvalid { get; } = Error.Create("ops.auth.link_invalid");

    public static Error PasswordTooShort { get; } = Error.Create("ops.auth.password_too_short", ("minLength", OperationsPasswordPolicy.MinLength));

    public static Error PasswordTooLong { get; } = Error.Create("ops.auth.password_too_long", ("maxLength", OperationsPasswordPolicy.MaxLength));

    public static Error PasswordTooCommon { get; } = Error.Create("ops.auth.password_too_common");

    public static Error PasswordContainsEmail { get; } = Error.Create("ops.auth.password_contains_email");
}
