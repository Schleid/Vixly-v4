namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantBillingSummary.cs
//
// PURPOSE:
// A tenant's open balance for the tenant page.
//
// RESPONSIBILITIES:
// - Carry totals per currency
//
// IMPORTANT NOTES:
// - One row per currency the tenant was billed in
// =========================================================
public sealed record PlatformTenantBillingSummary(
    string CurrencyCode,
    decimal OpenBalance,
    decimal OverdueBalance,
    int OpenInvoices);
