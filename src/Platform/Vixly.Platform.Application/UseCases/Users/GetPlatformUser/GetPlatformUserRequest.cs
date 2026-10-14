namespace Vixly.Platform.Application.UseCases.Users.GetPlatformUser;

// =========================================================
// FILE: GetPlatformUserRequest.cs
//
// PURPOSE:
// Input for GetPlatformUser.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Viewing any staff user needs platform.users.view
// =========================================================
public sealed class GetPlatformUserRequest
{
    public Guid PlatformUserId { get; init; }
}
