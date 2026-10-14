namespace Vixly.Platform.Application.UseCases.Users.SuspendPlatformUser;

// =========================================================
// FILE: SuspendPlatformUserRequest.cs
//
// PURPOSE:
// Input for SuspendPlatformUser.
//
// RESPONSIBILITIES:
// - Name the staff user and give the reason
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context, never
//   from the request (S-2)
// =========================================================
public sealed class SuspendPlatformUserRequest
{
    public Guid PlatformUserId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
