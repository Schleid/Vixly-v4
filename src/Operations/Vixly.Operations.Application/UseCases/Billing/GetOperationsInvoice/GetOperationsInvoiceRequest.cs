namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoice;

// =========================================================
// FILE: GetOperationsInvoiceRequest.cs
//
// PURPOSE:
// One invoice with its lines, taxes and credit notes (Slice 5F, I-2, I-6 and I-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsInvoiceRequest
{
    public Guid InvoiceId { get; init; }
}
