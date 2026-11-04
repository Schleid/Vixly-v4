using Vixly.Operations.Application.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditAccounts;

// =========================================================
// FILE: ListOperationsCreditAccountsResponse.cs
//
// PURPOSE:
// The credit department's accounts (Slice 5F step 2b, K-6): each with its customer, what is owed, available and overdue.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsCreditAccountsResponse
{
    private ListOperationsCreditAccountsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCreditRow> Rows { get; private init; } = Array.Empty<OperationsCreditRow>();

    public int Awaiting { get; private init; }

    public bool CreditOn { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    // Customers marked for credit before accounts existed, still without one (review).
    public IReadOnlyList<(Guid CustomerId, string? Code, string Name)> Marked { get; private init; } = Array.Empty<(Guid, string?, string)>();

    public static ListOperationsCreditAccountsResponse Success(IReadOnlyList<OperationsCreditRow> rows, int awaiting, bool creditOn, string currencyCode,
        IReadOnlyList<(Guid CustomerId, string? Code, string Name)> marked)
    {
        return new ListOperationsCreditAccountsResponse { Succeeded = true, Rows = rows, Awaiting = awaiting, CreditOn = creditOn, CurrencyCode = currencyCode, Marked = marked };
    }

    public static ListOperationsCreditAccountsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsCreditAccountsResponse { Error = error };
    }
}
