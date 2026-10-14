using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentListState.cs
//
// PURPOSE:
// What the departments list shows.
//
// RESPONSIBILITIES:
// - Hold the departments, whether the user may create one, and
//   any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record DepartmentListState(
    bool Loaded,
    IReadOnlyList<PlatformDepartmentListItem> Departments,
    bool CanCreate,
    Error? Error)
{
    public static DepartmentListState Initial { get; } = new(false, Array.Empty<PlatformDepartmentListItem>(), false, null);
}
