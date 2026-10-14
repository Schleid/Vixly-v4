namespace Vixly.Platform.Application.UseCases.Tenants.ActivatePlatformTenant;

// =========================================================
// FILE: ActivatePlatformTenantRequest.cs
//
// PURPOSE:
// Input for ActivatePlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - No input but the tenant
// =========================================================
public sealed class ActivatePlatformTenantRequest
{
    public Guid PlatformTenantId { get; init; }
}
