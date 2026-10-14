namespace Vixly.Platform.Application.UseCases.Roles.UpdateRolePermissions;

// =========================================================
// FILE: UpdateRolePermissionsRequest.cs
//
// PURPOSE:
// Input for UpdateRolePermissions.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class UpdateRolePermissionsRequest
{
    public Guid PlatformRoleId { get; init; }

    public IReadOnlyList<string> PermissionCodes { get; init; } = Array.Empty<string>();
}
