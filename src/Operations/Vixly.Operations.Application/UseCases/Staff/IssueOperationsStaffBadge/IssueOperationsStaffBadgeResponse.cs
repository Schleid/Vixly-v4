using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge;

// =========================================================
// FILE: IssueOperationsStaffBadgeResponse.cs
//
// PURPOSE:
// Issues a new badge to a staff member, revoking the one in use (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class IssueOperationsStaffBadgeResponse
{
    private IssueOperationsStaffBadgeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Code { get; private init; } = string.Empty;

    public string PersonName { get; private init; } = string.Empty;

    public static IssueOperationsStaffBadgeResponse Success(string code, string personName)
    {
        return new IssueOperationsStaffBadgeResponse { Succeeded = true, Code = code, PersonName = personName };
    }

    public static IssueOperationsStaffBadgeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new IssueOperationsStaffBadgeResponse { Error = error };
    }
}
