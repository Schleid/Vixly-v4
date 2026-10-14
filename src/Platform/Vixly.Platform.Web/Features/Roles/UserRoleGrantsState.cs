using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: UserRoleGrantsState.cs
//
// PURPOSE:
// What the roles section of a user shows.
//
// RESPONSIBILITIES:
// - Hold the grants, the roles that can be given, whether the user
//   may change them, the pending change, the last result and any
//   error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - DoneKey is a text key (UI-10)
// =========================================================
public sealed record UserRoleGrantsState(
    bool Loaded,
    IReadOnlyList<PlatformUserRoleItem> Grants,
    IReadOnlyList<PlatformRoleListItem> Assignable,
    bool CanManage,
    PendingRoleChange? Pending,
    string? DoneKey,
    Error? Error)
{
    public static UserRoleGrantsState Initial { get; } =
        new(false, Array.Empty<PlatformUserRoleItem>(), Array.Empty<PlatformRoleListItem>(), false, null, null, null);
}
