namespace Vixly.Platform.Application.UseCases.Registration.ResendMyRegistrationVerification;

// =========================================================
// FILE: ResendMyRegistrationVerificationRequest.cs
//
// PURPOSE:
// Input for ResendMyRegistrationVerification.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The access token Public.Web holds
// =========================================================
public sealed class ResendMyRegistrationVerificationRequest
{
    public string AccessToken { get; init; } = string.Empty;
}
