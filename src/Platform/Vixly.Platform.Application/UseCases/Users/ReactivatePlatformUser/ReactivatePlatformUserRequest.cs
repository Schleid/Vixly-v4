namespace Vixly.Platform.Application.UseCases.Users.ReactivatePlatformUser;

// =========================================================
// FILE: ReactivatePlatformUserRequest.cs
//
// PURPOSE:
// Input for ReactivatePlatformUser.
//
// RESPONSIBILITIES:
// - Name the staff user and give the reason
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context, never
//   from the request (S-2)
// =========================================================
public sealed class ReactivatePlatformUserRequest
{
    public Guid PlatformUserId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
