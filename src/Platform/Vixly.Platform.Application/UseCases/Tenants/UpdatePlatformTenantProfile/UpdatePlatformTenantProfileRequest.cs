namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantProfile;

// =========================================================
// FILE: UpdatePlatformTenantProfileRequest.cs
//
// PURPOSE:
// Input for UpdatePlatformTenantProfile.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain
// =========================================================
public sealed class UpdatePlatformTenantProfileRequest
{
    public Guid PlatformTenantId { get; init; }

    public PlatformCompanyInput Company { get; init; } = new();
}
