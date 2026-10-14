using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Tenants.SuspendPlatformTenant;

// =========================================================
// FILE: SuspendPlatformTenantRequest.cs
//
// PURPOSE:
// Input for SuspendPlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A reason is required
// =========================================================
public sealed class SuspendPlatformTenantRequest
{
    public Guid PlatformTenantId { get; init; }

    public PlatformSuspensionCategory Category { get; init; }

    public string Reason { get; init; } = string.Empty;
}
