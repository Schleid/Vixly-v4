namespace Vixly.Operations.Application.UseCases.Roles.SetOperationsRoleActive;

// =========================================================
// FILE: SetOperationsRoleActiveRequest.cs
//
// PURPOSE:
// Deactivates or reactivates a role (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetOperationsRoleActiveRequest
{
    public Guid RoleId { get; init; }

    public bool Active { get; init; }
}
