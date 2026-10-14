namespace Vixly.Platform.Application.UseCases.Departments;

// =========================================================
// FILE: PlatformDepartmentActions.cs
//
// PURPOSE:
// What the signed-in user may do to one department.
//
// RESPONSIBILITIES:
// - Tell the department page which controls to offer
//
// IMPORTANT NOTES:
// - A convenience for the UI: every use case checks again
// =========================================================
public sealed record PlatformDepartmentActions(
    bool CanEdit,
    bool CanDeactivate,
    bool CanReactivate,
    bool CanManageMembers)
{
    public static PlatformDepartmentActions None { get; } = new(false, false, false, false);
}
