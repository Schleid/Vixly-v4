namespace Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;

// =========================================================
// FILE: VerifyRegistrationEmailRequest.cs
//
// PURPOSE:
// Input for VerifyRegistrationEmail.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - From the verification link
// =========================================================
public sealed class VerifyRegistrationEmailRequest
{
    public string Token { get; init; } = string.Empty;
}
