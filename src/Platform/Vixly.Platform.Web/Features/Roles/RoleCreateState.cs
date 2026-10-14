using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RoleCreateState.cs
//
// PURPOSE:
// What the new role form shows.
//
// RESPONSIBILITIES:
// - Hold whether the user may create a role, the new id, and any
//   error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record RoleCreateState(bool Loaded, bool CanCreate, Guid? CreatedRoleId, Error? Error)
{
    public static RoleCreateState Initial { get; } = new(false, false, null, null);
}
