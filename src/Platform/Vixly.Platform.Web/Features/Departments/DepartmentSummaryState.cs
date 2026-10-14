using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Departments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentSummaryState.cs
//
// PURPOSE:
// What the department summary shows.
//
// RESPONSIBILITIES:
// - Hold the department, the allowed actions, which form is open,
//   the last result and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - DoneKey is a text key (UI-10)
// =========================================================
public sealed record DepartmentSummaryState(
    PlatformDepartmentDetail? Department,
    PlatformDepartmentActions Actions,
    bool Editing,
    bool ConfirmingDeactivate,
    string? DoneKey,
    Error? Error)
{
    public static DepartmentSummaryState Initial { get; } = new(null, PlatformDepartmentActions.None, false, false, null, null);
}
