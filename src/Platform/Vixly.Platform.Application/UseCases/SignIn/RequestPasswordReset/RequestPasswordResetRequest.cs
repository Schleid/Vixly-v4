namespace Vixly.Platform.Application.UseCases.SignIn.RequestPasswordReset;

// =========================================================
// FILE: RequestPasswordResetRequest.cs
//
// PURPOSE:
// Input for RequestPasswordReset.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint, rate limited per client IP (S-6)
// =========================================================
public sealed class RequestPasswordResetRequest
{
    public string Email { get; init; } = string.Empty;
}
