using Vixly.Operations.Application.Charges;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.ListOperationsQuotes;

// =========================================================
// FILE: ListOperationsQuotesResponse.cs
//
// PURPOSE:
// The company's latest quotes, with their customers (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsQuotesResponse
{
    private ListOperationsQuotesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsQuoteRow> Quotes { get; private init; } = Array.Empty<OperationsQuoteRow>();

    public int TotalCount { get; private init; }

    public OperationsQuoteCounts Counts { get; private init; } = new(0, 0, 0);

    public int Page { get; private init; } = 1;

    public int PageSize { get; private init; } = ListOperationsQuotesRequest.DefaultPageSize;

    // When the list was read: a quote past its date shows as expired.
    public DateTimeOffset UtcNow { get; private init; }

    public static ListOperationsQuotesResponse Success(IReadOnlyList<OperationsQuoteRow> quotes, int totalCount, OperationsQuoteCounts counts, int page, int pageSize,
        DateTimeOffset utcNow)
    {
        return new ListOperationsQuotesResponse { Succeeded = true, Quotes = quotes, TotalCount = totalCount, Counts = counts, Page = page, PageSize = pageSize, UtcNow = utcNow };
    }

    public static ListOperationsQuotesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsQuotesResponse { Error = error };
    }
}
