using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.GetPlatformUserDepartments;

// =========================================================
// FILE: GetPlatformUserDepartmentsResponse.cs
//
// PURPOSE:
// Result of GetPlatformUserDepartments.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformUserDepartmentsResponse
{
    private GetPlatformUserDepartmentsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformUserDepartmentItem> Memberships { get; private init; } = Array.Empty<PlatformUserDepartmentItem>();

    public IReadOnlyList<PlatformDepartmentListItem> Joinable { get; private init; } = Array.Empty<PlatformDepartmentListItem>();

    public bool CanManage { get; private init; }

    public static GetPlatformUserDepartmentsResponse Success(IReadOnlyList<PlatformUserDepartmentItem> memberships, IReadOnlyList<PlatformDepartmentListItem> joinable, bool canManage)
    {
        return new GetPlatformUserDepartmentsResponse { Succeeded = true, Memberships = memberships, Joinable = joinable, CanManage = canManage };
    }

    public static GetPlatformUserDepartmentsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformUserDepartmentsResponse { Error = error };
    }
}
