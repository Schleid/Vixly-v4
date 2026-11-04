namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsInvoiceDocument;

// =========================================================
// FILE: GetMyOperationsInvoiceDocumentRequest.cs
//
// PURPOSE:
// The PDF of one of the customer's own invoices or approved credit notes (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsInvoiceDocumentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid InvoiceId { get; init; }

    public Guid? CreditNoteId { get; init; }
}
