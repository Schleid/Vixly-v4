using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.AddPlatformDepartmentMember;

// =========================================================
// FILE: AddPlatformDepartmentMemberResponse.cs
//
// PURPOSE:
// Result of AddPlatformDepartmentMember.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddPlatformDepartmentMemberResponse
{
    private AddPlatformDepartmentMemberResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AddPlatformDepartmentMemberResponse Success()
    {
        return new AddPlatformDepartmentMemberResponse { Succeeded = true };
    }

    public static AddPlatformDepartmentMemberResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddPlatformDepartmentMemberResponse { Error = error };
    }
}
