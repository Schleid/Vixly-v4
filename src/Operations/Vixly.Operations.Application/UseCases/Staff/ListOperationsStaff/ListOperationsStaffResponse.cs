using Vixly.Operations.Application.Staff;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.ListOperationsStaff;

// =========================================================
// FILE: ListOperationsStaffResponse.cs
//
// PURPOSE:
// Lists the staff of the tenant, by page (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsStaffResponse
{
    private ListOperationsStaffResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsStaffMember> Members { get; private init; } = Array.Empty<OperationsStaffMember>();

    public int TotalCount { get; private init; }

    public static ListOperationsStaffResponse Success(IReadOnlyList<OperationsStaffMember> members, int totalCount)
    {
        return new ListOperationsStaffResponse { Succeeded = true, Members = members, TotalCount = totalCount };
    }

    public static ListOperationsStaffResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsStaffResponse { Error = error };
    }
}
