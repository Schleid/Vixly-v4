namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithPassword;

// =========================================================
// FILE: CompleteOperationsSignInWithPasswordRequest.cs
//
// PURPOSE:
// Finishes the daily check of a badge or PIN sign-in with the password, for a person without an authenticator (Slice 12A).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompleteOperationsSignInWithPasswordRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
