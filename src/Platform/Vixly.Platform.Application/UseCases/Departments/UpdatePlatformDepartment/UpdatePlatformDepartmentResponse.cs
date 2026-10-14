using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.UpdatePlatformDepartment;

// =========================================================
// FILE: UpdatePlatformDepartmentResponse.cs
//
// PURPOSE:
// Result of UpdatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformDepartmentResponse
{
    private UpdatePlatformDepartmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformDepartmentResponse Success()
    {
        return new UpdatePlatformDepartmentResponse { Succeeded = true };
    }

    public static UpdatePlatformDepartmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformDepartmentResponse { Error = error };
    }
}
