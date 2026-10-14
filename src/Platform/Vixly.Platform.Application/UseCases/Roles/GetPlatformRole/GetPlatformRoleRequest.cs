namespace Vixly.Platform.Application.UseCases.Roles.GetPlatformRole;

// =========================================================
// FILE: GetPlatformRoleRequest.cs
//
// PURPOSE:
// Input for GetPlatformRole.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class GetPlatformRoleRequest
{
    public Guid PlatformRoleId { get; init; }
}
