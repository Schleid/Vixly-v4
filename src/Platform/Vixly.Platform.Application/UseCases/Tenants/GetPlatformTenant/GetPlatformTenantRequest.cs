namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenant;

// =========================================================
// FILE: GetPlatformTenantRequest.cs
//
// PURPOSE:
// Input for GetPlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:

// =========================================================
public sealed class GetPlatformTenantRequest
{
    public Guid PlatformTenantId { get; init; }
}
