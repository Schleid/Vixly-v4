namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;

// =========================================================
// FILE: GetPlatformTenantEntitlementsRequest.cs
//
// PURPOSE:
// Reads what a tenant may use: access, features and effective limits.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantEntitlementsRequest
{
    public Guid PlatformTenantId { get; init; }
}
