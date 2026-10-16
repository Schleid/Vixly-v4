namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformTenantBilling;

// =========================================================
// FILE: GetPlatformTenantBillingRequest.cs
//
// PURPOSE:
// Reads a tenant's open balance, tax details and the tax rate that applies (tenant page).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantBillingRequest
{
    public Guid PlatformTenantId { get; init; }
}
