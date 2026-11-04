using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsIdentityErrors.cs
//
// PURPOSE:
// Error codes of the staff identity model (Slice 1).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code with its arguments
//
// IMPORTANT NOTES:
// - Codes start ops.identity. (L-1); texts live in OperationsText
// =========================================================
public static class OperationsIdentityErrors
{
    public static Error NameRequired { get; } = Error.Create("ops.identity.name_required");

    public static Error NameTooLong { get; } = Error.Create("ops.identity.name_too_long", ("maxLength", OperationsIdentityLimits.NameMaxLength));

    public static Error PasswordHashRequired { get; } = Error.Create("ops.identity.password_hash_required");

    public static Error InvitationNotPending { get; } = Error.Create("ops.identity.invitation_not_pending");

    public static Error AccountNotActive { get; } = Error.Create("ops.identity.account_not_active");

    public static Error AuthenticatorSecretInvalid { get; } = Error.Create("ops.identity.authenticator_secret_invalid");

    public static Error AuthenticatorNotPending { get; } = Error.Create("ops.identity.authenticator_not_pending");

    public static Error AuthenticatorNotConfirmed { get; } = Error.Create("ops.identity.authenticator_not_confirmed");

    public static Error AuthenticatorCodeReused { get; } = Error.Create("ops.identity.authenticator_code_reused");

    public static Error RecoveryCodesInvalid { get; } = Error.Create("ops.identity.recovery_codes_invalid");

    public static Error RecoveryCodeNotAccepted { get; } = Error.Create("ops.identity.recovery_code_not_accepted");

    public static Error TokenHashInvalid { get; } = Error.Create("ops.identity.token_hash_invalid");

    public static Error TokenExpired { get; } = Error.Create("ops.identity.token_expired");

    public static Error TokenAlreadyUsed { get; } = Error.Create("ops.identity.token_already_used");

    public static Error TokenPurposeInvalid { get; } = Error.Create("ops.identity.token_purpose_invalid");

    public static Error SessionEnded { get; } = Error.Create("ops.identity.session_ended");

    public static Error UserRequired { get; } = Error.Create("ops.identity.user_required");

    public static Error SignInChallengeInvalid { get; } = Error.Create("ops.identity.sign_in_challenge_invalid");

    public static Error AuditActionInvalid { get; } = Error.Create("ops.identity.audit_action_invalid");

    public static Error AuditTargetInvalid { get; } = Error.Create("ops.identity.audit_target_invalid");
}
