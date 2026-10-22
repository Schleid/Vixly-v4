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

namespace Vixly.Operations.Application.UseCases.Cashier.StartOperationsCardPayment;

// =========================================================
// FILE: StartOperationsCardPaymentUseCase.cs
//
// PURPOSE:
// Starts a card payment for an invoice at the cashier: Stripe's payment page (QR code or customer screen) or a card reader (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.payments.take at the invoice’s warehouse
// - A new charge cancels the one started before for the same invoice
// - Paid only on Stripe’s word (OperationsCardDesk)
// =========================================================
public sealed class StartOperationsCardPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsCardRepository _store;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsSettingsRepository _settings;

    public StartOperationsCardPaymentUseCase(
        OperationsBillingDesk desk,
        OperationsCardDesk cards,
        IOperationsCardRepository store,
        IOperationsBillingRepository billing,
        IOperationsSettingsRepository settings)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<StartOperationsCardPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StartOperationsCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return StartOperationsCardPaymentResponse.Failure(refused);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return StartOperationsCardPaymentResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // Partner agents take payments only where their partner handles money (R-8).
        if (!access!.HandlesMoneyAt(warehouses.First(warehouse => warehouse.Id == invoice.WarehouseId)))
        {
            return StartOperationsCardPaymentResponse.Failure(Vixly.Operations.Domain.Pickup.OperationsPickupErrors.MoneyNotHandled);
        }

        if (await _cards.SecretKeyAsync(cancellationToken) is not { } secret || !OperationsCardDesk.Offered(await _settings.FindAsync(cancellationToken), true))
        {
            return StartOperationsCardPaymentResponse.Failure(OperationsCardErrors.NotConnected);
        }

        OperationsCardReader? reader = null;

        if (request.Channel == OperationsCardChannel.Reader
            && (request.ReaderId is not { } readerId || (reader = await _store.GetReaderAsync(readerId, cancellationToken)) is null || reader.WarehouseId != invoice.WarehouseId))
        {
            return StartOperationsCardPaymentResponse.Failure(OperationsCardErrors.ReaderInvalid);
        }

        if (request.Channel is not (OperationsCardChannel.PaymentPage or OperationsCardChannel.Reader)
            || (request.Channel == OperationsCardChannel.PaymentPage && !request.ReturnUrlPrefix.StartsWith("https://", StringComparison.Ordinal)))
        {
            return StartOperationsCardPaymentResponse.Failure(OperationsCommonErrors.RequestInvalid);
        }

        (OperationsCardCharge? charge, string? _, Error? failed) = await _cards.StartAsync(secret, invoice, request.Channel, reader, access.User.Id, invoice.Party.CustomerEmail,
            id => request.ReturnUrlPrefix + id.ToString("D"), cancellationToken);
        return charge is null ? StartOperationsCardPaymentResponse.Failure(failed!) : StartOperationsCardPaymentResponse.Success(charge.Id);
    }
}
