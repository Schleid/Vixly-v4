namespace Vixly.Platform.Application.UseCases.Users.ResetPlatformUserSecondFactor;

// =========================================================
// FILE: ResetPlatformUserSecondFactorRequest.cs
//
// PURPOSE:
// Input for ResetPlatformUserSecondFactor.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ResetPlatformUserSecondFactorRequest
{
    public Guid PlatformUserId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
