namespace Vixly.Platform.Application.UseCases.Roles.AssignPlatformRole;

// =========================================================
// FILE: AssignPlatformRoleRequest.cs
//
// PURPOSE:
// Input for AssignPlatformRole.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class AssignPlatformRoleRequest
{
    public Guid PlatformUserId { get; init; }

    public Guid PlatformRoleId { get; init; }

    public DateTimeOffset? ExpiresUtc { get; init; }
}
