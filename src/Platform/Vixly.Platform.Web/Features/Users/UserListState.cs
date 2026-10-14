using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserListState.cs
//
// PURPOSE:
// What the user list shows.
//
// RESPONSIBILITIES:
// - Carry one page of users, the paging facts, whether the
//   Invite button shows, and the error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record UserListState(
    IReadOnlyList<PlatformUserListItem> Users,
    int Page,
    int TotalPages,
    int TotalCount,
    bool CanInvite,
    bool Loaded,
    Error? Error)
{
    public static UserListState Initial { get; } = new(Array.Empty<PlatformUserListItem>(), 1, 0, 0, false, false, null);
}
