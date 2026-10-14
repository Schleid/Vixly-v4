namespace Vixly.Platform.Application.UseCases.Registrations.ResendRegistrationVerification;

// =========================================================
// FILE: ResendRegistrationVerificationRequest.cs
//
// PURPOSE:
// Input for ResendRegistrationVerification.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - No input but the registration
// =========================================================
public sealed class ResendRegistrationVerificationRequest
{
    public Guid RegistrationId { get; init; }
}
