namespace Vixly.Contracts.Platform.Internal.Billing;

// =========================================================
// FILE: PlatformInvoiceInfo.cs
//
// PURPOSE:
// One of the tenant's invoices (call 22).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Status: issued, paid, void
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformInvoiceInfo(Guid InvoiceId, string Number, string Kind, string Status, string CurrencyCode, decimal Total, decimal Balance, decimal PaidTotal, DateTimeOffset IssuedUtc, DateTimeOffset DueUtc, bool Overdue, bool CreditPending);
