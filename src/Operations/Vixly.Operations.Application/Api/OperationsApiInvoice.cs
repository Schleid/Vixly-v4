namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiInvoice.cs
//
// PURPOSE:
// An invoice as the company API shows it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry what the company API and its webhooks show
//
// IMPORTANT NOTES:
// - Ids, codes and figures only: never a password, a protected value or staff notes
// - A field is only ever added, never renamed or removed (receivers depend on it)
// =========================================================
public sealed record OperationsApiInvoice(
    Guid Id,
    string Number,
    string Kind,
    Guid CustomerId,
    Guid? ShipmentId,
    string CurrencyCode,
    DateOnly IssuedOn,
    DateOnly? DueOn,
    string Status,
    string PaymentState,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    decimal CreditedTotal,
    decimal PaidTotal,
    decimal Owed,
    DateTimeOffset CreatedUtc);
