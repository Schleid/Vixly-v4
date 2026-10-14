using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments;

// =========================================================
// FILE: PlatformDepartmentErrors.cs
//
// PURPOSE:
// Error codes for department administration.
//
// RESPONSIBILITIES:
// - Name the refusals the department use cases return
//
// IMPORTANT NOTES:
// - Codes only; text lives in PlatformText (L-1)
// =========================================================
public static class PlatformDepartmentErrors
{
    public static readonly Error NameAlreadyUsed = Error.Create("platform.departments.name_already_used");

    public static readonly Error DepartmentInactive = Error.Create("platform.departments.department_inactive");
}
