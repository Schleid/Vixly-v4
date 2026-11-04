namespace Vixly.Operations.Application.UseCases.Roles.SaveOperationsRole;

// =========================================================
// FILE: SaveOperationsRoleRequest.cs
//
// PURPOSE:
// Creates a role or changes its name and permissions (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsRoleRequest
{
    public Guid? RoleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
