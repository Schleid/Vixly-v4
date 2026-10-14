using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.SetPrimaryPlatformDepartment;

// =========================================================
// FILE: SetPrimaryPlatformDepartmentResponse.cs
//
// PURPOSE:
// Result of SetPrimaryPlatformDepartment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPrimaryPlatformDepartmentResponse
{
    private SetPrimaryPlatformDepartmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPrimaryPlatformDepartmentResponse Success()
    {
        return new SetPrimaryPlatformDepartmentResponse { Succeeded = true };
    }

    public static SetPrimaryPlatformDepartmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPrimaryPlatformDepartmentResponse { Error = error };
    }
}
