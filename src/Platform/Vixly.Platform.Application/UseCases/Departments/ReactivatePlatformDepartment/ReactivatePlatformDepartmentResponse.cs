using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.ReactivatePlatformDepartment;

// =========================================================
// FILE: ReactivatePlatformDepartmentResponse.cs
//
// PURPOSE:
// Result of ReactivatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReactivatePlatformDepartmentResponse
{
    private ReactivatePlatformDepartmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReactivatePlatformDepartmentResponse Success()
    {
        return new ReactivatePlatformDepartmentResponse { Succeeded = true };
    }

    public static ReactivatePlatformDepartmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReactivatePlatformDepartmentResponse { Error = error };
    }
}
