namespace Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactorWithPasskey;

// =========================================================
// FILE: ConfirmSecondFactorWithPasskeyRequest.cs
//
// PURPOSE:
// Input for ConfirmSecondFactorWithPasskey.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The session comes from the context, never the request
// =========================================================
public sealed class ConfirmSecondFactorWithPasskeyRequest
{
    public string CeremonyToken { get; init; } = string.Empty;

    public string AssertionJson { get; init; } = string.Empty;
}
