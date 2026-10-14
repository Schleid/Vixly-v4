namespace Vixly.Platform.Application.UseCases.Roles.GetPlatformUserRoles;

// =========================================================
// FILE: GetPlatformUserRolesRequest.cs
//
// PURPOSE:
// Input for GetPlatformUserRoles.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class GetPlatformUserRolesRequest
{
    public Guid PlatformUserId { get; init; }
}
