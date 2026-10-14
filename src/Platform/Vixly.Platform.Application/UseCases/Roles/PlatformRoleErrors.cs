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
    public static Error NameAlreadyUsed { get; } = Error.Create("platform.roles.name_already_used");

    public static Error RoleInactive { get; } = Error.Create("platform.roles.role_inactive");

    public static Error ExceedsYourAccess { get; } = Error.Create("platform.roles.exceeds_your_access");
}
