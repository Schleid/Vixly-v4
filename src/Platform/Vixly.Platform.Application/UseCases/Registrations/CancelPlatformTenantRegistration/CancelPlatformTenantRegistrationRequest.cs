namespace Vixly.Platform.Application.UseCases.Registrations.CancelPlatformTenantRegistration;

// =========================================================
// FILE: CancelPlatformTenantRegistrationRequest.cs
//
// PURPOSE:
// Input for CancelPlatformTenantRegistration.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A reason is required
// =========================================================
public sealed class CancelPlatformTenantRegistrationRequest
{
    public Guid RegistrationId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
