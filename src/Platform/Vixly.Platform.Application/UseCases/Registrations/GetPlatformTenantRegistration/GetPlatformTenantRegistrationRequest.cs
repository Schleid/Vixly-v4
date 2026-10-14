namespace Vixly.Platform.Application.UseCases.Registrations.GetPlatformTenantRegistration;

// =========================================================
// FILE: GetPlatformTenantRegistrationRequest.cs
//
// PURPOSE:
// Input for GetPlatformTenantRegistration.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:

// =========================================================
public sealed class GetPlatformTenantRegistrationRequest
{
    public Guid RegistrationId { get; init; }
}
