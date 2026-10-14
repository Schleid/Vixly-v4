namespace Vixly.Platform.Application.UseCases.Tenants.ReactivatePlatformTenant;

// =========================================================
// FILE: ReactivatePlatformTenantRequest.cs
//
// PURPOSE:
// Input for ReactivatePlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A reason is required
// =========================================================
public sealed class ReactivatePlatformTenantRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
