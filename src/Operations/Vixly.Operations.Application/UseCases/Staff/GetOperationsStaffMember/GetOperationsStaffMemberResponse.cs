using Vixly.Operations.Application.Staff;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsStaffMember;

// =========================================================
// FILE: GetOperationsStaffMemberResponse.cs
//
// PURPOSE:
// Reads one staff member with their roles (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsStaffMemberResponse
{
    private GetOperationsStaffMemberResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsStaffMember? Member { get; private init; }

    public static GetOperationsStaffMemberResponse Success(OperationsStaffMember? member)
    {
        return new GetOperationsStaffMemberResponse { Succeeded = true, Member = member };
    }

    public static GetOperationsStaffMemberResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsStaffMemberResponse { Error = error };
    }
}
