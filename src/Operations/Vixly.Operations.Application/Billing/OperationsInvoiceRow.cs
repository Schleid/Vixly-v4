using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsInvoiceRow.cs
//
// PURPOSE:
// One invoice in a list (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Carry what the list shows
//
// IMPORTANT NOTES:
// - Amounts in the invoice's currency
// =========================================================
public sealed record OperationsInvoiceRow(
    Guid InvoiceId,
    string Number,
    OperationsInvoiceKind Kind,
    string Reference,
    string Customer,
    DateOnly IssuedOn,
    string CurrencyCode,
    decimal Total,
    decimal Credited,
    decimal Owed,
    bool Voided,
    string WarehouseCode,
    decimal Pending = 0m);
