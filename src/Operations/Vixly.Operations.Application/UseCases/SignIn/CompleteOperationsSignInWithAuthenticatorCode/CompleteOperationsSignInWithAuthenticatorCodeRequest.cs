namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;

// =========================================================
// FILE: CompleteOperationsSignInWithAuthenticatorCodeRequest.cs
//
// PURPOSE:
// Finishes staff sign-in with an authenticator app code (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompleteOperationsSignInWithAuthenticatorCodeRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
