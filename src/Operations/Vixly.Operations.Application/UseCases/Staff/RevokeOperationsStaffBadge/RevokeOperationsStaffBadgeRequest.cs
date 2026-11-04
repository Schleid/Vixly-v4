namespace Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge;

// =========================================================
// FILE: RevokeOperationsStaffBadgeRequest.cs
//
// PURPOSE:
// Revokes a staff member's badge, for example when it is lost (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RevokeOperationsStaffBadgeRequest
{
    public Guid UserId { get; init; }
}
