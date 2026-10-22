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

    // Owner, 2026-10-21: a device signing in from another country than its own must confirm with a second factor; this account has none.
    public static Error CountryNeedsSecondFactor { get; } = Error.Create("ops.auth.country_needs_second_factor");

    // Slice 7 step 4 (R-8): partner agents are not part of the company's plan now, or Platform could not say so.
    public static Error PartnerAgentsOff { get; } = Error.Create("ops.auth.partner_agents_off");

    public static Error CodeInvalid { get; } = Error.Create("ops.auth.code_invalid");

    public static Error SignInExpired { get; } = Error.Create("ops.auth.sign_in_expired");

    public static Error LinkInvalid { get; } = Error.Create("ops.auth.link_invalid");

    public static Error PasswordTooShort { get; } = Error.Create("ops.auth.password_too_short", ("minLength", OperationsPasswordPolicy.MinLength));

    public static Error PasswordTooLong { get; } = Error.Create("ops.auth.password_too_long", ("maxLength", OperationsPasswordPolicy.MaxLength));

    public static Error PasswordTooCommon { get; } = Error.Create("ops.auth.password_too_common");

    public static Error PasswordContainsEmail { get; } = Error.Create("ops.auth.password_contains_email");
}
