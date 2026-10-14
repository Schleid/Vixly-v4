using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformIdentityErrors.cs
//
// PURPOSE:
// The catalogue of errors raised by the Platform identity
// model: users, accounts, roles and departments.
//
// RESPONSIBILITIES:
// - Declare every identity error code in one place
//
// IMPORTANT NOTES:
// - Codes start with platform.identity. (Global Standard 4.2)
// - Each code needs en, fr and es text in
//   Vixly.Platform.Resources (L-10)
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformIdentityErrors
{
    public static Error NameRequired { get; } =
        Error.Create("platform.identity.name_required");

    public static Error NameTooLong { get; } =
        Error.Create("platform.identity.name_too_long", ("maxLength", PlatformIdentityLimits.NameMaxLength));

    public static Error EmployeeNumberTooLong { get; } =
        Error.Create(
            "platform.identity.employee_number_too_long",
            ("maxLength", PlatformIdentityLimits.EmployeeNumberMaxLength));

    public static Error AuthorityLevelInvalid { get; } =
        Error.Create("platform.identity.authority_level_invalid");

    public static Error PasswordHashRequired { get; } =
        Error.Create("platform.identity.password_hash_required");

    public static Error InvitationNotPending { get; } =
        Error.Create("platform.identity.invitation_not_pending");

    public static Error AccountNotActive { get; } =
        Error.Create("platform.identity.account_not_active");

    public static Error AccountNotRestricted { get; } =
        Error.Create("platform.identity.account_not_restricted");

    public static Error AccountDeactivated { get; } =
        Error.Create("platform.identity.account_deactivated");

    public static Error ReasonRequired { get; } =
        Error.Create("platform.identity.reason_required");

    public static Error ReasonTooLong { get; } =
        Error.Create("platform.identity.reason_too_long", ("maxLength", PlatformIdentityLimits.ReasonMaxLength));

    public static Error ActorRequired { get; } =
        Error.Create("platform.identity.actor_required");

    public static Error CannotActOnOwnAccount { get; } =
        Error.Create("platform.identity.cannot_act_on_own_account");

    public static Error LastSuperUser { get; } =
        Error.Create("platform.identity.last_super_user");

    public static Error SuperUserCountInvalid { get; } =
        Error.Create("platform.identity.super_user_count_invalid");

    public static Error RoleRequired { get; } =
        Error.Create("platform.identity.role_required");

    public static Error RoleAlreadyAssigned { get; } =
        Error.Create("platform.identity.role_already_assigned");

    public static Error RoleNotAssigned { get; } =
        Error.Create("platform.identity.role_not_assigned");

    public static Error AssignmentExpiryNotInFuture { get; } =
        Error.Create("platform.identity.assignment_expiry_not_in_future");

    public static Error SystemRoleProtected { get; } =
        Error.Create("platform.identity.system_role_protected");

    public static Error SystemRoleKeyInvalid { get; } =
        Error.Create("platform.identity.system_role_key_invalid");

    public static Error RoleAlreadyActive { get; } =
        Error.Create("platform.identity.role_already_active");

    public static Error RoleAlreadyInactive { get; } =
        Error.Create("platform.identity.role_already_inactive");

    public static Error PermissionCodeInvalid { get; } =
        Error.Create("platform.identity.permission_code_invalid");

    public static Error DepartmentRequired { get; } =
        Error.Create("platform.identity.department_required");

    public static Error DepartmentAlreadyActive { get; } =
        Error.Create("platform.identity.department_already_active");

    public static Error DepartmentAlreadyInactive { get; } =
        Error.Create("platform.identity.department_already_inactive");

    public static Error DescriptionTooLong { get; } =
        Error.Create(
            "platform.identity.description_too_long",
            ("maxLength", PlatformIdentityLimits.DescriptionMaxLength));

    public static Error AlreadyDepartmentMember { get; } =
        Error.Create("platform.identity.already_department_member");

    public static Error NotDepartmentMember { get; } =
        Error.Create("platform.identity.not_department_member");

    public static Error PasskeyInvalid { get; } =
        Error.Create("platform.identity.passkey_invalid");

    public static Error PasskeyAlreadyRegistered { get; } =
        Error.Create("platform.identity.passkey_already_registered");

    public static Error PasskeyLimitReached { get; } =
        Error.Create("platform.identity.passkey_limit_reached", ("maximum", PlatformIdentityLimits.MaxPasskeysPerUser));

    public static Error PasskeyNotFound { get; } =
        Error.Create("platform.identity.passkey_not_found");

    public static Error PasskeyCounterRegressed { get; } =
        Error.Create("platform.identity.passkey_counter_regressed");

    public static Error LastSecondFactor { get; } =
        Error.Create("platform.identity.last_second_factor");

    public static Error AuthenticatorSecretInvalid { get; } =
        Error.Create("platform.identity.authenticator_secret_invalid");

    public static Error AuthenticatorNotPending { get; } =
        Error.Create("platform.identity.authenticator_not_pending");

    public static Error AuthenticatorNotConfirmed { get; } =
        Error.Create("platform.identity.authenticator_not_confirmed");

    public static Error AuthenticatorCodeReused { get; } =
        Error.Create("platform.identity.authenticator_code_reused");

    public static Error RecoveryCodesInvalid { get; } =
        Error.Create("platform.identity.recovery_codes_invalid", ("count", PlatformIdentityLimits.RecoveryCodeCount));

    public static Error RecoveryCodeNotAccepted { get; } =
        Error.Create("platform.identity.recovery_code_not_accepted");

    public static Error TokenHashInvalid { get; } =
        Error.Create("platform.identity.token_hash_invalid");

    public static Error TokenExpired { get; } =
        Error.Create("platform.identity.token_expired");

    public static Error TokenAlreadyUsed { get; } =
        Error.Create("platform.identity.token_already_used");

    public static Error TokenPurposeInvalid { get; } =
        Error.Create("platform.identity.token_purpose_invalid");

    public static Error SessionEnded { get; } =
        Error.Create("platform.identity.session_ended");

    public static Error UserRequired { get; } =
        Error.Create("platform.identity.user_required");

    public static Error AuditActionInvalid { get; } =
        Error.Create("platform.identity.audit_action_invalid");

    public static Error AuditTargetInvalid { get; } =
        Error.Create("platform.identity.audit_target_invalid");

    public static Error SignInChallengeInvalid { get; } =
        Error.Create("platform.identity.sign_in_challenge_invalid");

    public static Error PasskeyCeremonyInvalid { get; } =
        Error.Create("platform.identity.passkey_ceremony_invalid");
}
