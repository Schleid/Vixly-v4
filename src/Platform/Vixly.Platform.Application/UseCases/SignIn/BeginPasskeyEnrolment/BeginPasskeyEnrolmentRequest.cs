namespace Vixly.Platform.Application.UseCases.SignIn.BeginPasskeyEnrolment;

// =========================================================
// FILE: BeginPasskeyEnrolmentRequest.cs
//
// PURPOSE:
// Input for BeginPasskeyEnrolment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The caller is anonymous: the sign-in challenge cookie proves
//   who it is, at the enrolment stage only
// =========================================================
public sealed class BeginPasskeyEnrolmentRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
}
