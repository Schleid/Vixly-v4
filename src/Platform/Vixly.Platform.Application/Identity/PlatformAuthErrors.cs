using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformAuthErrors.cs
//
// PURPOSE:
// Errors of the sign-in, invitation and password flows.
//
// RESPONSIBILITIES:
// - Declare the codes the sign-in screens show
//
// IMPORTANT NOTES:
// - SignInFailed is deliberately vague: it never says whether
//   the email exists, the password was wrong or the account
//   is locked (no account discovery)
// - LinkInvalid covers unknown, expired, used and revoked
//   links alike
// =========================================================
public static class PlatformAuthErrors
{
    public static Error SignInFailed { get; } =
        Error.Create("platform.auth.sign_in_failed");

    public static Error CodeInvalid { get; } =
        Error.Create("platform.auth.code_invalid");

    public static Error SignInExpired { get; } =
        Error.Create("platform.auth.sign_in_expired");

    public static Error LinkInvalid { get; } =
        Error.Create("platform.auth.link_invalid");

    public static Error PasswordTooShort { get; } =
        Error.Create("platform.auth.password_too_short", ("minLength", PlatformPasswordPolicy.MinLength));

    public static Error PasswordTooLong { get; } =
        Error.Create("platform.auth.password_too_long", ("maxLength", PlatformPasswordPolicy.MaxLength));

    public static Error PasswordTooCommon { get; } =
        Error.Create("platform.auth.password_too_common");

    public static Error PasswordContainsEmail { get; } =
        Error.Create("platform.auth.password_contains_email");

    public static Error PasswordIncorrect { get; } =
        Error.Create("platform.auth.password_incorrect");

    public static Error SetupAlreadyDone { get; } =
        Error.Create("platform.auth.setup_already_done");

    public static Error AuthorityNotAllowed { get; } =
        Error.Create("platform.auth.authority_not_allowed");

    public static Error TargetAuthorityHigher { get; } =
        Error.Create("platform.auth.target_authority_higher");

    public static Error SessionEnded { get; } =
        Error.Create("platform.auth.session_ended");

    public static Error AccountNotInvited { get; } =
        Error.Create("platform.auth.account_not_invited");

    public static Error CurrentSessionNotEnded { get; } =
        Error.Create("platform.auth.current_session_not_ended");

    public static Error NoPasskeys { get; } =
        Error.Create("platform.auth.no_passkeys");

    public static Error PasskeyNotAccepted { get; } =
        Error.Create("platform.auth.passkey_not_accepted");
}
