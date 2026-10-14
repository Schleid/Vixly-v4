namespace Vixly.Platform.Application.UseCases.Users.LockPlatformUser;

// =========================================================
// FILE: LockPlatformUserRequest.cs
//
// PURPOSE:
// Input for LockPlatformUser.
//
// RESPONSIBILITIES:
// - Name the staff user and give the reason
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context, never
//   from the request (S-2)
// =========================================================
public sealed class LockPlatformUserRequest
{
    public Guid PlatformUserId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
