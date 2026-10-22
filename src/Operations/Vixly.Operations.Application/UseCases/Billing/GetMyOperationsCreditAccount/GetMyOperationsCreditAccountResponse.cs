using Vixly.Operations.Domain.Billing;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditAccount;

// =========================================================
// FILE: GetMyOperationsCreditAccountResponse.cs
//
// PURPOSE:
// The customer's own credit account on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsCreditAccountResponse
{
    private GetMyOperationsCreditAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCreditAccount? Account { get; private init; }

    public IReadOnlyList<OperationsInvoice> Items { get; private init; } = Array.Empty<OperationsInvoice>();

    public OperationsCreditPosition Position { get; private init; } = null!;

    public int TermsDays { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static GetMyOperationsCreditAccountResponse Success(OperationsCreditAccount? account, IReadOnlyList<OperationsInvoice> items, OperationsCreditPosition position, int termsDays, string currencyCode,
        DateOnly today)
    {
        return new GetMyOperationsCreditAccountResponse { Succeeded = true, Account = account, Items = items, Position = position, TermsDays = termsDays, CurrencyCode = currencyCode, Today = today };
    }

    // The company's day, so each item's overdue mark agrees with the totals (review).
    public DateOnly Today { get; private init; }

    public static GetMyOperationsCreditAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsCreditAccountResponse { Error = error };
    }
}
