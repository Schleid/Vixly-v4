using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.CreatePlatformDepartment;

// =========================================================
// FILE: CreatePlatformDepartmentResponse.cs
//
// PURPOSE:
// Result of CreatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformDepartmentResponse
{
    private CreatePlatformDepartmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformDepartmentId { get; private init; }

    public static CreatePlatformDepartmentResponse Success(Guid platformDepartmentId)
    {
        return new CreatePlatformDepartmentResponse { Succeeded = true, PlatformDepartmentId = platformDepartmentId };
    }

    public static CreatePlatformDepartmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformDepartmentResponse { Error = error };
    }
}
