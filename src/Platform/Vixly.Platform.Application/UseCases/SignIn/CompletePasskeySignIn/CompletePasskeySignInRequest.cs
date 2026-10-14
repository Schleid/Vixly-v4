namespace Vixly.Platform.Application.UseCases.SignIn.CompletePasskeySignIn;

// =========================================================
// FILE: CompletePasskeySignInRequest.cs
//
// PURPOSE:
// Input for CompletePasskeySignIn.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The token comes from the ceremony cookie, the answer from the
//   browser
// =========================================================
public sealed class CompletePasskeySignInRequest
{
    public string CeremonyToken { get; init; } = string.Empty;

    public string AssertionJson { get; init; } = string.Empty;
}
