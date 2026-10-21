namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceDocument;

// =========================================================
// FILE: GetOperationsInvoiceDocumentRequest.cs
//
// PURPOSE:
// The PDF of an invoice or of one of its approved credit notes (Slice 5F, I-2 and I-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsInvoiceDocumentRequest
{
    public Guid InvoiceId { get; init; }

    // None: the invoice itself.
    public Guid? CreditNoteId { get; init; }
}
