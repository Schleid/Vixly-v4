namespace Vixly.Operations.Application.UseCases.Charges.GetOperationsQuoteDocument;

// =========================================================
// FILE: GetOperationsQuoteDocumentRequest.cs
//
// PURPOSE:
// The quote to print (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Carry the input of the use case
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsQuoteDocumentRequest
{
    public Guid QuoteId { get; init; }
}
