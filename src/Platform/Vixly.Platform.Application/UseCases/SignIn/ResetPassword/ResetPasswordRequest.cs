namespace Vixly.Platform.Application.UseCases.SignIn.ResetPassword;

// =========================================================
// FILE: ResetPasswordRequest.cs
//
// PURPOSE:
// Input for ResetPassword.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint, opened from the reset email
// =========================================================
public sealed class ResetPasswordRequest
{
    public string Token { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
