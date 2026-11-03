namespace Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge;

// =========================================================
// FILE: IssueOperationsStaffBadgeRequest.cs
//
// PURPOSE:
// Issues a new badge to a staff member, revoking the one in use (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class IssueOperationsStaffBadgeRequest
{
    public Guid UserId { get; init; }
}
