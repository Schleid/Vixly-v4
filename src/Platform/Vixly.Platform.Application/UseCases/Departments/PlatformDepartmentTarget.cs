using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments;

// =========================================================
// FILE: PlatformDepartmentTarget.cs
//
// PURPOSE:
// The department a department use case acts on, or why it may
// not.
//
// RESPONSIBILITIES:
// - Carry the loaded department or the refusal
//
// IMPORTANT NOTES:
// - Exactly one of Department and Error is set
// =========================================================
public sealed record PlatformDepartmentTarget(PlatformDepartment? Department, Error? Error)
{
    public static PlatformDepartmentTarget Found(PlatformDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new PlatformDepartmentTarget(department, null);
    }

    public static PlatformDepartmentTarget Refused(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlatformDepartmentTarget(null, error);
    }
}
