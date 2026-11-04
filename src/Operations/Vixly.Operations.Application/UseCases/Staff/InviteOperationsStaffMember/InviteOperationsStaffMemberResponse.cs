using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.InviteOperationsStaffMember;

// =========================================================
// FILE: InviteOperationsStaffMemberResponse.cs
//
// PURPOSE:
// Invites a new staff member with a first role and emails the 72-hour setup link (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class InviteOperationsStaffMemberResponse
{
    private InviteOperationsStaffMemberResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid UserId { get; private init; }

    public static InviteOperationsStaffMemberResponse Success(Guid userId)
    {
        return new InviteOperationsStaffMemberResponse { Succeeded = true, UserId = userId };
    }

    public static InviteOperationsStaffMemberResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new InviteOperationsStaffMemberResponse { Error = error };
    }
}
