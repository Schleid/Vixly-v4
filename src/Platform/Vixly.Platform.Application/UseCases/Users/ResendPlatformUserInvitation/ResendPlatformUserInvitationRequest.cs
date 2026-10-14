namespace Vixly.Platform.Application.UseCases.Users.ResendPlatformUserInvitation;

// =========================================================
// FILE: ResendPlatformUserInvitationRequest.cs
//
// PURPOSE:
// Input for ResendPlatformUserInvitation.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ResendPlatformUserInvitationRequest
{
    public Guid PlatformUserId { get; init; }
}
