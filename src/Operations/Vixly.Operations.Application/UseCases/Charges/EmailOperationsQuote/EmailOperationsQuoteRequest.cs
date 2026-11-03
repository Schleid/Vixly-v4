namespace Vixly.Operations.Application.UseCases.Charges.EmailOperationsQuote;

// =========================================================
// FILE: EmailOperationsQuoteRequest.cs
//
// PURPOSE:
// The quote to email to its customer (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Carry the input of the use case
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EmailOperationsQuoteRequest
{
    public Guid QuoteId { get; init; }
}
