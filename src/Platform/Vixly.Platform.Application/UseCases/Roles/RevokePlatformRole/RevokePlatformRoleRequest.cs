namespace Vixly.Platform.Application.UseCases.Roles.RevokePlatformRole;

// =========================================================
// FILE: RevokePlatformRoleRequest.cs
//
// PURPOSE:
// Input for RevokePlatformRole.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class RevokePlatformRoleRequest
{
    public Guid PlatformUserId { get; init; }

    public Guid PlatformRoleId { get; init; }
}
