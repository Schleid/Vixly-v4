namespace Vixly.Platform.Application.UseCases.Tenants.DeactivatePlatformTenant;

// =========================================================
// FILE: DeactivatePlatformTenantRequest.cs
//
// PURPOSE:
// Input for DeactivatePlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A reason is required
// =========================================================
public sealed class DeactivatePlatformTenantRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
