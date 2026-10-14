namespace Vixly.Platform.Application.UseCases.Roles.DeactivatePlatformRole;

// =========================================================
// FILE: DeactivatePlatformRoleRequest.cs
//
// PURPOSE:
// Input for DeactivatePlatformRole.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class DeactivatePlatformRoleRequest
{
    public Guid PlatformRoleId { get; init; }
}
