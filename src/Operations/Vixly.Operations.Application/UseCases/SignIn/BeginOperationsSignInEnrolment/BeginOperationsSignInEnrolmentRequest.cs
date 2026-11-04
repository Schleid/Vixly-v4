namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;

// =========================================================
// FILE: BeginOperationsSignInEnrolmentRequest.cs
//
// PURPOSE:
// Starts authenticator app enrolment during sign-in, for staff who need a second factor and have none (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BeginOperationsSignInEnrolmentRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
}
