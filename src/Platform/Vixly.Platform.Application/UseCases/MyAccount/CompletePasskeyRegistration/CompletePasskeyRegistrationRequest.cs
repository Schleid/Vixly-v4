namespace Vixly.Platform.Application.UseCases.MyAccount.CompletePasskeyRegistration;

// =========================================================
// FILE: CompletePasskeyRegistrationRequest.cs
//
// PURPOSE:
// Input for CompletePasskeyRegistration.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Name is what the user calls this passkey ("Work laptop")
// =========================================================
public sealed class CompletePasskeyRegistrationRequest
{
    public string CeremonyToken { get; init; } = string.Empty;

    public string AttestationJson { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
