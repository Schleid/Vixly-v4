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
    public static Error NameAlreadyUsed { get; } = Error.Create("platform.departments.name_already_used");

    public static Error DepartmentInactive { get; } = Error.Create("platform.departments.department_inactive");
}
