using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformInvoiceListItem.cs
//
// PURPOSE:
// One row of the invoices list.
//
// RESPONSIBILITIES:
// - Carry what the list shows
//
// IMPORTANT NOTES:
// - Overdue is computed from the due date
// =========================================================
public sealed record PlatformInvoiceListItem(
    Guid PlatformInvoiceId,
    string Number,
    Guid PlatformTenantId,
    string TenantName,
    PlatformInvoiceKind Kind,
    PlatformInvoiceStatus Status,
    string CurrencyCode,
    decimal Total,
    decimal Balance,
    decimal PaidTotal,
    DateTimeOffset IssuedUtc,
    DateTimeOffset DueUtc,
    bool Overdue,
    bool CreditPending);
