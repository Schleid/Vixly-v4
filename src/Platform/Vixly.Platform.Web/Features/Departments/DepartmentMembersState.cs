using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentMembersState.cs
//
// PURPOSE:
// What the members list of a department shows.
//
// RESPONSIBILITIES:
// - Hold the members, whether the user may remove them, the last
//   result and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record DepartmentMembersState(
    bool Loaded,
    IReadOnlyList<PlatformDepartmentMemberItem> Members,
    bool CanManage,
    bool Removed,
    Error? Error)
{
    public static DepartmentMembersState Initial { get; } = new(false, Array.Empty<PlatformDepartmentMemberItem>(), false, false, null);
}
