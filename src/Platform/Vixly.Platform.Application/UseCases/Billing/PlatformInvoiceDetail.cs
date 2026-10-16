using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformInvoiceDetail.cs
//
// PURPOSE:
// An invoice with its lines, payments, credit notes and history.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformInvoiceDetail(
    Guid PlatformInvoiceId,
    string Number,
    Guid PlatformTenantId,
    string TenantName,
    Guid? PlatformSubscriptionId,
    PlatformInvoiceKind Kind,
    PlatformInvoiceStatus Status,
    string CurrencyCode,
    string Language,
    string CustomerName,
    string CustomerAddress,
    string? CustomerTaxId,
    string CustomerEmail,
    DateTimeOffset IssuedUtc,
    DateTimeOffset DueUtc,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    decimal NetTotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal Total,
    decimal PaidTotal,
    decimal CreditedTotal,
    decimal Balance,
    decimal RefundOwed,
    bool Overdue,
    IReadOnlyList<PlatformInvoiceLineItem> Lines,
    IReadOnlyList<PlatformPaymentItem> Payments,
    IReadOnlyList<PlatformCreditNoteItem> Credits,
    IReadOnlyList<PlatformInvoiceEventItem> History);
