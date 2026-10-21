using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Billing.ListMyOperationsInvoices;

// =========================================================
// FILE: ListMyOperationsInvoicesUseCase.cs
//
// PURPOSE:
// The customer's own invoices and their approved credit notes (Slice 5F, I-8), on the portal.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer's own only; at most 100, newest first
// - Requests waiting for a decision are staff matters and never shown
// =========================================================
public sealed class ListMyOperationsInvoicesUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsBillingRepository _billing;

    public ListMyOperationsInvoicesUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsBillingRepository billing)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<ListMyOperationsInvoicesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListMyOperationsInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ListMyOperationsInvoicesResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        IReadOnlyList<OperationsInvoice> invoices = await _billing.ListCustomerInvoicesAsync(signedIn.Customer.Id, 100, cancellationToken);
        return ListMyOperationsInvoicesResponse.Success(invoices,
            await _billing.ListApprovedCreditNotesAsync(invoices.Select(invoice => invoice.Id).ToList(), cancellationToken));
    }
}
