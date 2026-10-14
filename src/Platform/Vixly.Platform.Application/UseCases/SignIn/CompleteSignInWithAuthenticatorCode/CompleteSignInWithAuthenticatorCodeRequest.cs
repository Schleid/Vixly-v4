namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithAuthenticatorCode;

// =========================================================
// FILE: CompleteSignInWithAuthenticatorCodeRequest.cs
//
// PURPOSE:
// Input for CompleteSignInWithAuthenticatorCode.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint: the pending sign-in identifies the user
// =========================================================
public sealed class CompleteSignInWithAuthenticatorCodeRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
