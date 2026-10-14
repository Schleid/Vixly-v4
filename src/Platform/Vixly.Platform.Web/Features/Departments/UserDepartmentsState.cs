using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: UserDepartmentsState.cs
//
// PURPOSE:
// What the departments section of a user shows.
//
// RESPONSIBILITIES:
// - Hold the memberships, the departments the user can join,
//   whether they may be changed, the last result and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - DoneKey is a text key (UI-10)
// =========================================================
public sealed record UserDepartmentsState(
    bool Loaded,
    IReadOnlyList<PlatformUserDepartmentItem> Memberships,
    IReadOnlyList<PlatformDepartmentListItem> Joinable,
    bool CanManage,
    string? DoneKey,
    Error? Error)
{
    public static UserDepartmentsState Initial { get; } =
        new(false, Array.Empty<PlatformUserDepartmentItem>(), Array.Empty<PlatformDepartmentListItem>(), false, null, null);
}
