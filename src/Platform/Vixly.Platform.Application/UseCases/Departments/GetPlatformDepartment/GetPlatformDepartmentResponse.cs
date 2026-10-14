using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.GetPlatformDepartment;

// =========================================================
// FILE: GetPlatformDepartmentResponse.cs
//
// PURPOSE:
// Result of GetPlatformDepartment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformDepartmentResponse
{
    private GetPlatformDepartmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformDepartmentDetail? Department { get; private init; }

    public PlatformDepartmentActions Actions { get; private init; } = PlatformDepartmentActions.None;

    public static GetPlatformDepartmentResponse Success(PlatformDepartmentDetail? department, PlatformDepartmentActions actions)
    {
        return new GetPlatformDepartmentResponse { Succeeded = true, Department = department, Actions = actions };
    }

    public static GetPlatformDepartmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformDepartmentResponse { Error = error };
    }
}
