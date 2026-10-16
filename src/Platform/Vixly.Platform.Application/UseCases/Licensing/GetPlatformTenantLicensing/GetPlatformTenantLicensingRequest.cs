namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformTenantLicensing;

// =========================================================
// FILE: GetPlatformTenantLicensingRequest.cs
//
// PURPOSE:
// Reads the Licensing section of a tenant page: seats, devices by state and actions (D-L15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantLicensingRequest
{
    public Guid PlatformTenantId { get; init; }
}
