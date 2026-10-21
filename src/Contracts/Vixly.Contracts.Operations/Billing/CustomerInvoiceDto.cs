namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerInvoiceDto.cs
//
// PURPOSE:
// One of the customer's invoices (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Carry the number, what it is for, the amounts and its approved credit notes
//
// IMPORTANT NOTES:
// - Kind: "shipment" or "order"; amounts in the invoice's currency
// =========================================================
public sealed record CustomerInvoiceDto(
    Guid InvoiceId,
    string Number,
    string Kind,
    string Reference,
    DateOnly IssuedOn,
    string CurrencyCode,
    decimal Total,
    decimal Credited,
    decimal Owed,
    bool Voided,
    IReadOnlyList<CustomerCreditNoteDto> CreditNotes,
    string State = "unpaid");
