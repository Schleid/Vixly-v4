namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsInvoice.cs
//
// PURPOSE:
// An invoice Vixly issued to the company.
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsInvoice(Guid InvoiceId, string Number, string Status, string CurrencyCode, decimal Total, decimal Balance, DateTimeOffset IssuedUtc, DateTimeOffset DueUtc, bool Overdue, bool CreditPending);
