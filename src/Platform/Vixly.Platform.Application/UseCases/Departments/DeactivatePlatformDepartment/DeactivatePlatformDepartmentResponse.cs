using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.DeactivatePlatformDepartment;

// =========================================================
// FILE: DeactivatePlatformDepartmentResponse.cs
//
// PURPOSE:
// Result of DeactivatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DeactivatePlatformDepartmentResponse
{
    private DeactivatePlatformDepartmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DeactivatePlatformDepartmentResponse Success()
    {
        return new DeactivatePlatformDepartmentResponse { Succeeded = true };
    }

    public static DeactivatePlatformDepartmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DeactivatePlatformDepartmentResponse { Error = error };
    }
}
