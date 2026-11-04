using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditAccount;

// =========================================================
// FILE: GetOperationsCreditAccountResponse.cs
//
// PURPOSE:
// A customer's credit account (Slice 5F step 2b, K-6): its terms, its open items with their age, and what can be changed.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCreditAccountResponse
{
    private GetOperationsCreditAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomer Customer { get; private init; } = null!;

    public OperationsCreditAccount? Account { get; private init; }

    public IReadOnlyList<OperationsInvoice> Items { get; private init; } = Array.Empty<OperationsInvoice>();

    public OperationsCreditPosition Position { get; private init; } = null!;

    public int CompanyTermsDays { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public DateOnly Today { get; private init; }

    public bool CreditOn { get; private init; }

    public static GetOperationsCreditAccountResponse Success(OperationsCustomer customer, OperationsCreditAccount? account, IReadOnlyList<OperationsInvoice> items, OperationsCreditPosition position, int companyTermsDays, string currencyCode, DateOnly today, bool creditOn)
    {
        return new GetOperationsCreditAccountResponse { Succeeded = true, Customer = customer, Account = account, Items = items, Position = position, CompanyTermsDays = companyTermsDays, CurrencyCode = currencyCode, Today = today, CreditOn = creditOn };
    }

    public static GetOperationsCreditAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCreditAccountResponse { Error = error };
    }
}
