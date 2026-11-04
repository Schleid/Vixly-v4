namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsStaffMember;

// =========================================================
// FILE: GetOperationsStaffMemberRequest.cs
//
// PURPOSE:
// Reads one staff member with their roles (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsStaffMemberRequest
{
    public Guid UserId { get; init; }
}
