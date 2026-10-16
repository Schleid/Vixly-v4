namespace Vixly.Platform.Application.UseCases.Billing.SetPlatformTenantTaxDetails;

// =========================================================
// FILE: SetPlatformTenantTaxDetailsRequest.cs
//
// PURPOSE:
// Sets a tenant's tax ID and tax exemption (D-B16).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformTenantTaxDetailsRequest
{
    public Guid PlatformTenantId { get; init; }

    public string? TaxId { get; init; }

    public bool TaxExempt { get; init; }

    public string? ExemptionReason { get; init; }

    public string? ExemptionReference { get; init; }
}
