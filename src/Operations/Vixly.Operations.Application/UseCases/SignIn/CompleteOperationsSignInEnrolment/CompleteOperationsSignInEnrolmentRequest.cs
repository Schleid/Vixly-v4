namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;

// =========================================================
// FILE: CompleteOperationsSignInEnrolmentRequest.cs
//
// PURPOSE:
// Confirms the authenticator app, issues ten recovery codes and starts the session (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompleteOperationsSignInEnrolmentRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
