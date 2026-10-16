namespace Vixly.Platform.Application.UseCases.TenantAccount.GetPlatformEntitlementsBatch;

// =========================================================
// FILE: GetPlatformEntitlementsBatchRequest.cs
//
// PURPOSE:
// Entitlements for up to 100 tenants in one call, for Operations background jobs (D-K6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformEntitlementsBatchRequest
{
    public IReadOnlyList<Guid> PlatformTenantIds { get; init; } = Array.Empty<Guid>();
}
