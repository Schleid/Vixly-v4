using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsRoleErrors.cs
//
// PURPOSE:
// Error codes of roles and staff management (Slice 1D).
//
// RESPONSIBILITIES:
// - Name every refusal of a role or staff action
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText
// =========================================================
public static class OperationsRoleErrors
{
    public static Error NameRequired { get; } = Error.Create("ops.roles.name_required");

    public static Error NameTooLong { get; } = Error.Create("ops.roles.name_too_long", ("maxLength", OperationsAccessLimits.RoleNameMaxLength));

    public static Error NameTaken { get; } = Error.Create("ops.roles.name_taken");

    public static Error PermissionUnknown { get; } = Error.Create("ops.roles.permission_unknown");

    public static Error PermissionReserved { get; } = Error.Create("ops.roles.permission_reserved");

    public static Error SystemRoleLocked { get; } = Error.Create("ops.roles.system_role_locked");

    public static Error RoleInactive { get; } = Error.Create("ops.roles.role_inactive");

    public static Error ScopeInvalid { get; } = Error.Create("ops.roles.scope_invalid");

    public static Error GrantExists { get; } = Error.Create("ops.roles.grant_exists");

    public static Error ReasonRequired { get; } = Error.Create("ops.staff.reason_required", ("maxLength", OperationsAccessLimits.ReasonMaxLength));

    public static Error OwnerProtected { get; } = Error.Create("ops.staff.owner_protected");

    public static Error NotYourself { get; } = Error.Create("ops.staff.not_yourself");

    public static Error StatusChangeInvalid { get; } = Error.Create("ops.staff.status_change_invalid");

    public static Error TransferNeedsAdministrator { get; } = Error.Create("ops.staff.transfer_needs_administrator");

    // Slice 7 step 4 (R-8): a partner agent holds the partner agent's access at its warehouse and no role.
    public static Error PartnerAgentNoRoles { get; } = Error.Create("ops.staff.partner_agent_no_roles");
}
