using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Payments.ListPlatformCardTransactions;

// =========================================================
// FILE: ListPlatformCardTransactionsResponse.cs
//
// PURPOSE:
// Lists card charges and refunds, paged, with status, origin and dispute filters.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformCardTransactionsResponse
{
    private ListPlatformCardTransactionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformCardTransactionItem>? Transactions { get; private init; }

    public static ListPlatformCardTransactionsResponse Success(PagedResult<PlatformCardTransactionItem>? transactions)
    {
        return new ListPlatformCardTransactionsResponse { Succeeded = true, Transactions = transactions };
    }

    public static ListPlatformCardTransactionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformCardTransactionsResponse { Error = error };
    }
}
