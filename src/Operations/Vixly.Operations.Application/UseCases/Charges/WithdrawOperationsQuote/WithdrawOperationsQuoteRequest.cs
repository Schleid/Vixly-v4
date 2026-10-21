namespace Vixly.Operations.Application.UseCases.Charges.WithdrawOperationsQuote;

// =========================================================
// FILE: WithdrawOperationsQuoteRequest.cs
//
// PURPOSE:
// Withdraws a quote: it no longer counts (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class WithdrawOperationsQuoteRequest
{
    public Guid QuoteId { get; init; }
}
