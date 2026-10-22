namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;

// =========================================================
// FILE: CompleteOperationsSignInWithRecoveryCodeRequest.cs
//
// PURPOSE:
// Finishes staff sign-in with a recovery code (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompleteOperationsSignInWithRecoveryCodeRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
