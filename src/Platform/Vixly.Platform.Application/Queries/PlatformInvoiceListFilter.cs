using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformInvoiceListFilter.cs
//
// PURPOSE:
// Filters for the invoices list.
//
// RESPONSIBILITIES:
// - Carry the list filters
//
// IMPORTANT NOTES:
// - Search matches the invoice number or the company name
// =========================================================
public sealed record PlatformInvoiceListFilter(
    PlatformInvoiceStatus? Status,
    bool OverdueOnly,
    Guid? PlatformTenantId,
    string? Search,
    DateTimeOffset? IssuedFromUtc,
    DateTimeOffset? IssuedToUtc,
    DateTimeOffset UtcNow);
