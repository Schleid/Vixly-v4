using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCardPayment;

// =========================================================
// FILE: GetMyOperationsCardPaymentUseCase.cs
//
// PURPOSE:
// A customer's card payment as it now stands, when they come back from Stripe's page (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the customer’s own charge; another’s answers "not found" (S-3)
// =========================================================
public sealed class GetMyOperationsCardPaymentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsCardRepository _store;

    private readonly IOperationsBillingRepository _billing;

    public GetMyOperationsCardPaymentUseCase(
        OperationsCustomerAccounts accounts,
        OperationsCardDesk cards,
        IOperationsCardRepository store,
        IOperationsBillingRepository billing)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<GetMyOperationsCardPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsCardPaymentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _store.GetChargeAsync(request.ChargeId, cancellationToken) is not { } charge || charge.CustomerId != signedIn.Customer.Id
            || await _billing.GetInvoiceAsync(charge.InvoiceId, cancellationToken) is not { } invoice)
        {
            return GetMyOperationsCardPaymentResponse.Failure(OperationsCardErrors.ChargeNotFound);
        }

        if (charge.Status == OperationsCardChargeStatus.Started && await _cards.SecretKeyAsync(cancellationToken) is { } secret)
        {
            await _cards.RefreshAsync(secret, charge, cancellationToken);
        }

        return GetMyOperationsCardPaymentResponse.Success(invoice.Id, invoice.Number, charge.Status, charge.Amount, charge.CurrencyCode,
            charge.Status == OperationsCardChargeStatus.Started ? charge.PageUrl : null);
    }
}
