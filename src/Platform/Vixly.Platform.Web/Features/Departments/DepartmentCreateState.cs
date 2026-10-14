using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentCreateState.cs
//
// PURPOSE:
// What the new department form shows.
//
// RESPONSIBILITIES:
// - Hold whether the user may create a department, the new id,
//   and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record DepartmentCreateState(bool Loaded, bool CanCreate, Guid? CreatedDepartmentId, Error? Error)
{
    public static DepartmentCreateState Initial { get; } = new(false, false, null, null);
}
