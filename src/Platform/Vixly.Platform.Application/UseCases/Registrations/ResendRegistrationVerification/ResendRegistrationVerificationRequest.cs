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
// - Email is optional: when it differs from the owner's,
//   staff correct a mistyped address before the link goes out
// =========================================================
public sealed class ResendRegistrationVerificationRequest
{
    public Guid RegistrationId { get; init; }

    public string? Email { get; init; }
}
