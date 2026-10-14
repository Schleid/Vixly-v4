namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInEnrolment;

// =========================================================
// FILE: CompleteSignInEnrolmentRequest.cs
//
// PURPOSE:
// Input for CompleteSignInEnrolment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint: the pending sign-in identifies the user
// =========================================================
public sealed class CompleteSignInEnrolmentRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
