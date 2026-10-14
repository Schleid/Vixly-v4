namespace Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;

// =========================================================
// FILE: GetPlatformUserActionsRequest.cs
//
// PURPOSE:
// Input for GetPlatformUserActions.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class GetPlatformUserActionsRequest
{
    public Guid PlatformUserId { get; init; }
}
