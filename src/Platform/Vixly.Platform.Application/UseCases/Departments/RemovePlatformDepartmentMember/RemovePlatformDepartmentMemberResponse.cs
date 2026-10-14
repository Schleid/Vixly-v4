using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.RemovePlatformDepartmentMember;

// =========================================================
// FILE: RemovePlatformDepartmentMemberResponse.cs
//
// PURPOSE:
// Result of RemovePlatformDepartmentMember.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemovePlatformDepartmentMemberResponse
{
    private RemovePlatformDepartmentMemberResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemovePlatformDepartmentMemberResponse Success()
    {
        return new RemovePlatformDepartmentMemberResponse { Succeeded = true };
    }

    public static RemovePlatformDepartmentMemberResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemovePlatformDepartmentMemberResponse { Error = error };
    }
}
