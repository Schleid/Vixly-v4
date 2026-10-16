namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformTenantBillingDetail.cs
//
// PURPOSE:
// A tenant's balance and tax details for the tenant page.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformTenantBillingDetail(
    IReadOnlyList<Vixly.Platform.Application.Queries.PlatformTenantBillingSummary> Balances,
    string? TaxId,
    bool TaxExempt,
    string? TaxExemptionReason,
    string? TaxExemptionReference,
    string CountryCode,
    string? StateProvince,
    decimal TaxRate,
    string? TaxLabel,
    bool CanEditTax,
    bool CanBillCharges);
