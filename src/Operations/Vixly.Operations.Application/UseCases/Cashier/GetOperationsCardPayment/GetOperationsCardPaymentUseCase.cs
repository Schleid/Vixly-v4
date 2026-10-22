using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardPayment;

// =========================================================
// FILE: GetOperationsCardPaymentUseCase.cs
//
// PURPOSE:
// A card payment at the cashier as it now stands, asking Stripe while it waits (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.payments.take at the charge’s warehouse
// - Asking Stripe here is the safety net when its webhook is late or missing
// =========================================================
public sealed class GetOperationsCardPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsCardRepository _store;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    public GetOperationsCardPaymentUseCase(
        OperationsBillingDesk desk,
        OperationsCardDesk cards,
        IOperationsCardRepository store,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<GetOperationsCardPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCardPaymentResponse.Failure(refused);
        }

        // Partner agents only where their partner handles money (R-8).
        if (await _store.GetChargeAsync(request.ChargeId, cancellationToken) is not { } charge || warehouses.FirstOrDefault(warehouse => warehouse.Id == charge.WarehouseId) is not { } at
            || !access!.HandlesMoneyAt(at) || await _billing.GetInvoiceAsync(charge.InvoiceId, cancellationToken) is not { } invoice)
        {
            return GetOperationsCardPaymentResponse.Failure(OperationsCardErrors.ChargeNotFound);
        }

        if (charge.Status == OperationsCardChargeStatus.Started && await _cards.SecretKeyAsync(cancellationToken) is { } secret)
        {
            await _cards.RefreshAsync(secret, charge, cancellationToken);
        }

        OperationsPayment? payment = charge.PaymentId is { } paymentId ? await _payments.GetPaymentAsync(paymentId, cancellationToken) : null;
        OperationsCardReader? reader = charge.ReaderId is { } readerId ? await _store.GetReaderAsync(readerId, cancellationToken) : null;
        return GetOperationsCardPaymentResponse.Success(charge.Id, invoice.Id, invoice.Number, charge.Channel, charge.Status, charge.Amount, charge.CurrencyCode,
            charge.Status == OperationsCardChargeStatus.Started ? charge.PageUrl : null, reader?.Label, payment?.Number, payment?.Id);
    }
}
