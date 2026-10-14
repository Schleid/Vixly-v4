namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformPermissions.cs
//
// PURPOSE:
// The catalogue of Platform permission codes.
//
// RESPONSIBILITIES:
// - Declare every permission code in one place
// - List them for validation and for the role editor
//
// IMPORTANT NOTES:
// - The code is the identity of a permission; there is no
//   permission table (Slice 1 design note, section 2)
// - Codes follow the error-code style, plane first
//   (Global Standard 4.2). V3's 69 codes are carried over
//   slice by slice; each slice adds its own here
// - V3 had both X.Read and X.View in every area with no
//   difference in use; V4 keeps one, view
// - Display names and descriptions live in
//   Vixly.Platform.Resources (L-6), keyed by the code
// - Never rename a shipped code: roles store it
// - platform.users.change_authority is never granted through a
//   role: only the SuperUser authority level carries it
// =========================================================
public static class PlatformPermissions
{
    public const string UsersView = "platform.users.view";

    public const string UsersCreate = "platform.users.create";

    public const string UsersEdit = "platform.users.edit";

    public const string UsersInvite = "platform.users.invite";

    public const string UsersSendPasswordReset = "platform.users.send_password_reset";

    public const string UsersLock = "platform.users.lock";

    public const string UsersSuspend = "platform.users.suspend";

    public const string UsersReactivate = "platform.users.reactivate";

    public const string UsersDeactivate = "platform.users.deactivate";

    public const string UsersResetSecondFactor = "platform.users.reset_second_factor";

    public const string UsersEndSessions = "platform.users.end_sessions";

    public const string UsersChangeAuthority = "platform.users.change_authority";

    public const string RolesView = "platform.roles.view";

    public const string RolesCreate = "platform.roles.create";

    public const string RolesEdit = "platform.roles.edit";

    public const string RolesDeactivate = "platform.roles.deactivate";

    public const string RolesEditPermissions = "platform.roles.edit_permissions";

    public const string RolesAssign = "platform.roles.assign";

    public const string DepartmentsView = "platform.departments.view";

    public const string DepartmentsCreate = "platform.departments.create";

    public const string DepartmentsEdit = "platform.departments.edit";

    public const string DepartmentsDeactivate = "platform.departments.deactivate";

    public const string DepartmentsManageMembers = "platform.departments.manage_members";

    public const string AuditView = "platform.audit.view";

    public static IReadOnlyList<string> Catalogue { get; } = new[]
    {
        UsersView,
        UsersCreate,
        UsersEdit,
        UsersInvite,
        UsersSendPasswordReset,
        UsersLock,
        UsersSuspend,
        UsersReactivate,
        UsersDeactivate,
        UsersResetSecondFactor,
        UsersEndSessions,
        UsersChangeAuthority,
        RolesView,
        RolesCreate,
        RolesEdit,
        RolesDeactivate,
        RolesEditPermissions,
        RolesAssign,
        DepartmentsView,
        DepartmentsCreate,
        DepartmentsEdit,
        DepartmentsDeactivate,
        DepartmentsManageMembers,
        AuditView
    };

    public static IReadOnlyCollection<string> All { get; } = new HashSet<string>(Catalogue, StringComparer.Ordinal);

    public static IReadOnlyList<string> Grantable { get; } =
        Catalogue.Where(code => code != UsersChangeAuthority).ToArray();

    public static bool IsKnown(string? code)
    {
        return code is not null && All.Contains(code);
    }

    public static bool IsGrantable(string? code)
    {
        return IsKnown(code) && code != UsersChangeAuthority;
    }
}
