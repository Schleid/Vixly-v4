using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoiceByCard;

// =========================================================
// FILE: PayMyOperationsInvoiceByCardUseCase.cs
//
// PURPOSE:
// A customer pays one of their invoices by card on the portal, on Stripe's payment page (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer types the card on Stripe’s page, never on the portal (P-4)
// - Paid only on Stripe’s word: its webhook, or the check when the customer comes back
// =========================================================
public sealed class PayMyOperationsInvoiceByCardUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsSettingsRepository _settings;

    public PayMyOperationsInvoiceByCardUseCase(
        OperationsCustomerAccounts accounts,
        OperationsCardDesk cards,
        IOperationsBillingRepository billing,
        IOperationsSettingsRepository settings)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<PayMyOperationsInvoiceByCardResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PayMyOperationsInvoiceByCardRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return PayMyOperationsInvoiceByCardResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own invoice; another's answers "not found" (S-3).
        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || invoice.CustomerId != signedIn.Customer.Id)
        {
            return PayMyOperationsInvoiceByCardResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        if (await _cards.SecretKeyAsync(cancellationToken) is not { } secret || !OperationsCardDesk.Offered(await _settings.FindAsync(cancellationToken), true))
        {
            return PayMyOperationsInvoiceByCardResponse.Failure(OperationsCardErrors.NotConnected);
        }

        if (!request.ReturnUrlPrefix.StartsWith("https://", StringComparison.Ordinal))
        {
            return PayMyOperationsInvoiceByCardResponse.Failure(OperationsCommonErrors.RequestInvalid);
        }

        (OperationsCardCharge? charge, string? pageUrl, Error? failed) = await _cards.StartAsync(secret, invoice, OperationsCardChannel.Portal, null, null, signedIn.Customer.Email.Value,
            id => request.ReturnUrlPrefix + id.ToString("D"), cancellationToken);
        return charge is null || pageUrl is null ? PayMyOperationsInvoiceByCardResponse.Failure(failed ?? OperationsCardErrors.ProviderRefused) : PayMyOperationsInvoiceByCardResponse.Success(charge.Id, pageUrl);
    }
}
