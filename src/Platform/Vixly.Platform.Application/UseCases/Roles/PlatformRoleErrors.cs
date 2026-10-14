using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles;

// =========================================================
// FILE: PlatformRoleErrors.cs
//
// PURPOSE:
// Error codes for role administration.
//
// RESPONSIBILITIES:
// - Name the refusals the role use cases return
//
// IMPORTANT NOTES:
// - Codes only; text lives in PlatformText (L-1)
// =========================================================
public static class PlatformRoleErrors
{
    public static readonly Error NameAlreadyUsed = Error.Create("platform.roles.name_already_used");

    public static readonly Error RoleInactive = Error.Create("platform.roles.role_inactive");

    public static readonly Error ExceedsYourAccess = Error.Create("platform.roles.exceeds_your_access");
}
