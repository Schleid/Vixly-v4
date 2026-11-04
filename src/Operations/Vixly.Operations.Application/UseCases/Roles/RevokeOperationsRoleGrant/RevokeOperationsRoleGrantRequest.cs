namespace Vixly.Operations.Application.UseCases.Roles.RevokeOperationsRoleGrant;

// =========================================================
// FILE: RevokeOperationsRoleGrantRequest.cs
//
// PURPOSE:
// Takes a role back from a staff member (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RevokeOperationsRoleGrantRequest
{
    public Guid GrantId { get; init; }

    // When given, the grant must belong to this person (the page it was revoked from).
    public Guid? UserId { get; init; }
}
