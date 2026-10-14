namespace Vixly.Platform.Application.UseCases.Tenants.BanPlatformTenant;

// =========================================================
// FILE: BanPlatformTenantRequest.cs
//
// PURPOSE:
// Input for BanPlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A reason is required
// =========================================================
public sealed class BanPlatformTenantRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
