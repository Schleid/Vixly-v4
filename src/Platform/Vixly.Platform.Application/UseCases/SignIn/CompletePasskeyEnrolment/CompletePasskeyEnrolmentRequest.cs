namespace Vixly.Platform.Application.UseCases.SignIn.CompletePasskeyEnrolment;

// =========================================================
// FILE: CompletePasskeyEnrolmentRequest.cs
//
// PURPOSE:
// Input for CompletePasskeyEnrolment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The caller is anonymous: the sign-in challenge cookie proves
//   who it is, at the enrolment stage only
// =========================================================
public sealed class CompletePasskeyEnrolmentRequest
{
    public string ChallengeToken { get; init; } = string.Empty;

    public string CeremonyToken { get; init; } = string.Empty;

    public string AttestationJson { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
