namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformSystemRoles.cs
//
// PURPOSE:
// The protected system roles and the permissions code gives
// them.
//
// RESPONSIBILITIES:
// - Name each system role by its stable key
// - Define each role's permissions in code, so deploys keep
//   the database in step (SynchronizeSystemPermissions)
//
// IMPORTANT NOTES:
// - SuperUser is an authority level, not a role: a SuperUser
//   holds every permission
// - security_administrator owns identity, sessions and audit
// - administrator owns business administration and may view
//   staff, but never changes identity or security
// - Later slices extend administrator with their business
//   permissions
// =========================================================
public static class PlatformSystemRoles
{
    public const string SecurityAdministrator = "security_administrator";

    public const string Administrator = "administrator";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Definitions { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            [SecurityAdministrator] = new[]
            {
                PlatformPermissions.UsersView,
                PlatformPermissions.UsersCreate,
                PlatformPermissions.UsersEdit,
                PlatformPermissions.UsersInvite,
                PlatformPermissions.UsersSendPasswordReset,
                PlatformPermissions.UsersLock,
                PlatformPermissions.UsersSuspend,
                PlatformPermissions.UsersReactivate,
                PlatformPermissions.UsersDeactivate,
                PlatformPermissions.UsersResetSecondFactor,
                PlatformPermissions.UsersEndSessions,
                PlatformPermissions.RolesView,
                PlatformPermissions.RolesCreate,
                PlatformPermissions.RolesEdit,
                PlatformPermissions.RolesDeactivate,
                PlatformPermissions.RolesEditPermissions,
                PlatformPermissions.RolesAssign,
                PlatformPermissions.DepartmentsView,
                PlatformPermissions.DepartmentsCreate,
                PlatformPermissions.DepartmentsEdit,
                PlatformPermissions.DepartmentsDeactivate,
                PlatformPermissions.DepartmentsManageMembers,
                PlatformPermissions.AuditView
            },
            [Administrator] = new[]
            {
                PlatformPermissions.UsersView,
                PlatformPermissions.RolesView,
                PlatformPermissions.DepartmentsView,
                PlatformPermissions.DepartmentsManageMembers
            }
        };
}
