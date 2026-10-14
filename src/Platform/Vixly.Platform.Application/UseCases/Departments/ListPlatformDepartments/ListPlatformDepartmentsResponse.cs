using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Departments.ListPlatformDepartments;

// =========================================================
// FILE: ListPlatformDepartmentsResponse.cs
//
// PURPOSE:
// Result of ListPlatformDepartments.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformDepartmentsResponse
{
    private ListPlatformDepartmentsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformDepartmentListItem> Departments { get; private init; } = Array.Empty<PlatformDepartmentListItem>();

    public bool CanCreate { get; private init; }

    public static ListPlatformDepartmentsResponse Success(IReadOnlyList<PlatformDepartmentListItem> departments, bool canCreate)
    {
        return new ListPlatformDepartmentsResponse { Succeeded = true, Departments = departments, CanCreate = canCreate };
    }

    public static ListPlatformDepartmentsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformDepartmentsResponse { Error = error };
    }
}
