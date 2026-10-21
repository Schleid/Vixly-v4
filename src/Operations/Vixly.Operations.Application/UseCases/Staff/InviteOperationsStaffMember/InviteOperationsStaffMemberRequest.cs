using Vixly.Operations.Domain.Access;
namespace Vixly.Operations.Application.UseCases.Staff.InviteOperationsStaffMember;

// =========================================================
// FILE: InviteOperationsStaffMemberRequest.cs
//
// PURPOSE:
// Invites a new staff member with a first role and emails the 72-hour setup link (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class InviteOperationsStaffMemberRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Language { get; init; } = "en";

    public Guid RoleId { get; init; }

    public OperationsRoleScope Scope { get; init; }

    public Guid? ScopeId { get; init; }

    public string PortalSlug { get; init; } = string.Empty;
}
