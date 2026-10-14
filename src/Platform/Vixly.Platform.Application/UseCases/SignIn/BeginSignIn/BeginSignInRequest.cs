namespace Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;

// =========================================================
// FILE: BeginSignInRequest.cs
//
// PURPOSE:
// Input for the password step of a staff sign-in.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint, rate limited per client IP (S-6)
// =========================================================
public sealed class BeginSignInRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
