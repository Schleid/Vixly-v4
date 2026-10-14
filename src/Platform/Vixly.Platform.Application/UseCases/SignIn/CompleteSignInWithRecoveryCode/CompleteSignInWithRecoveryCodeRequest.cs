namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithRecoveryCode;

// =========================================================
// FILE: CompleteSignInWithRecoveryCodeRequest.cs
//
// PURPOSE:
// Input for CompleteSignInWithRecoveryCode.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint: the pending sign-in identifies the user
// =========================================================
public sealed class CompleteSignInWithRecoveryCodeRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
