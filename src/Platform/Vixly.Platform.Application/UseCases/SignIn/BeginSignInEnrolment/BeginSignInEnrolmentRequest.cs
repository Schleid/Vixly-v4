namespace Vixly.Platform.Application.UseCases.SignIn.BeginSignInEnrolment;

// =========================================================
// FILE: BeginSignInEnrolmentRequest.cs
//
// PURPOSE:
// Input for BeginSignInEnrolment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint: the pending sign-in identifies the user
// =========================================================
public sealed class BeginSignInEnrolmentRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
}
