using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsInvoiceDocument.cs
//
// PURPOSE:
// What the invoice PDF prints (Slice 5F, I-2 and I-6): the invoice, or one of its credit notes.
//
// RESPONSIBILITIES:
// - Carry the invoice and, for a credit note, the note
//
// IMPORTANT NOTES:
// - Printed in the invoice's language (the customer's at issue); the company's name only, never "Vixly"
// =========================================================
public sealed record OperationsInvoiceDocument(OperationsInvoice Invoice, OperationsCreditNote? CreditNote);
