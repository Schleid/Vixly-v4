using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RoleListState.cs
//
// PURPOSE:
// What the roles list shows.
//
// RESPONSIBILITIES:
// - Hold the roles, whether the user may create one, and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record RoleListState(
    bool Loaded,
    IReadOnlyList<PlatformRoleListItem> Roles,
    bool CanCreate,
    Error? Error)
{
    public static RoleListState Initial { get; } = new(false, Array.Empty<PlatformRoleListItem>(), false, null);
}
