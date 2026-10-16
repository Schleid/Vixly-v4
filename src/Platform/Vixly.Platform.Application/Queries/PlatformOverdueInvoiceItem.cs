namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformOverdueInvoiceItem.cs
//
// PURPOSE:
// An overdue invoice of a tenant, for the case page.
//
// RESPONSIBILITIES:
// - Carry number, due date, balance and whether a credit request waits
//
// IMPORTANT NOTES:
// - Only Issued invoices with a balance after their due date
// =========================================================
public sealed record PlatformOverdueInvoiceItem(
    Guid PlatformInvoiceId,
    string Number,
    DateTimeOffset DueUtc,
    decimal Balance,
    string CurrencyCode,
    bool CreditRequested);
