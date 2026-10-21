namespace Vixly.Operations.Application.UseCases.Charges.ListOperationsQuotes;

// =========================================================
// FILE: ListOperationsQuotesRequest.cs
//
// PURPOSE:
// The company's quotes, with their customers (Slice 4 design section 7); the quotes list filters by state and search, a page at a time (owner, 2026-10-20).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsQuotesRequest
{
    public const int DefaultPageSize = 50;

    // Null: every state.
    public Vixly.Operations.Application.Charges.OperationsQuoteState? State { get; init; }

    // A quote number, or a customer's name, code, email or phone.
    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
