namespace Vixly.Platform.Application.UseCases.Roles.UpdatePlatformRole;

// =========================================================
// FILE: UpdatePlatformRoleRequest.cs
//
// PURPOSE:
// Input for UpdatePlatformRole.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class UpdatePlatformRoleRequest
{
    public Guid PlatformRoleId { get; init; }

    public string Name { get; init; } = string.Empty;
}
