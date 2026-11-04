using Vixly.Operations.Domain.Access;
namespace Vixly.Operations.Application.UseCases.Roles.GrantOperationsRole;

// =========================================================
// FILE: GrantOperationsRoleRequest.cs
//
// PURPOSE:
// Gives a staff member a role for the tenant, a branch or a warehouse (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GrantOperationsRoleRequest
{
    public Guid UserId { get; init; }

    public Guid RoleId { get; init; }

    public OperationsRoleScope Scope { get; init; }

    public Guid? ScopeId { get; init; }
}
