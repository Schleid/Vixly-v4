using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.RefundOperationsPayment;

// =========================================================
// FILE: RefundOperationsPaymentUseCase.cs
//
// PURPOSE:
// Give money back after a credit note on a paid invoice (Slice 5F, F-7 and C-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.refund, at the invoice's warehouse
// - Cash comes out of the person's open drawer
// =========================================================
public sealed class RefundOperationsPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsCardDesk _cards;

    public RefundOperationsPaymentUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        OperationsNumbering numbering,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        OperationsCardDesk cards)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RefundOperationsPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RefundOperationsPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsRefund, cancellationToken);

        if (refused is not null)
        {
            return RefundOperationsPaymentResponse.Failure(refused);
        }

        if (request.RequestKey != Guid.Empty && await _payments.FindRefundByKeyAsync(request.RequestKey, cancellationToken) is { } already)
        {
            // Second review: a card refund left pending (Stripe silent, or the save after it failed) is sent again with the same key; Stripe answers
            // with the refund it already made, or makes it now.
            if (already.Pending && await _billing.GetInvoiceAsync(already.InvoiceId, cancellationToken) is { } pendingInvoice)
            {
                return await SendCardAsync(access!.User.Id, pendingInvoice, already, cancellationToken);
            }

            return RefundOperationsPaymentResponse.Success(already.Id);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return RefundOperationsPaymentResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // Second review: no money is given back where a partner does not handle it.
        if (!access!.HandlesMoneyAt(warehouses.First(warehouse => warehouse.Id == invoice.WarehouseId)))
        {
            return RefundOperationsPaymentResponse.Failure(Vixly.Operations.Domain.Pickup.OperationsPickupErrors.MoneyNotHandled);
        }

        if (request.Method == OperationsPaymentMethod.Card && OperationsCardCharge.HasCentsInWholeUnitCurrency(invoice.CurrencyCode, request.Amount))
        {
            return RefundOperationsPaymentResponse.Failure(OperationsPaymentErrors.AmountInvalid);
        }

        // Given back only by a method the company uses (review of 5F).
        if ((await _settings.FindAsync(cancellationToken)) is { } settings && !settings.Accepts(request.Method))
        {
            return RefundOperationsPaymentResponse.Failure(OperationsPaymentErrors.MethodNotAccepted);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsCashDrawer? drawer = await _payments.FindOpenDrawerAsync(access!.User.Id, invoice.WarehouseId, cancellationToken);
        Guid key = request.RequestKey == Guid.Empty ? Guid.NewGuid() : request.RequestKey;
        OperationsRefund refund;

        try
        {
            refund = OperationsRefund.Give(invoice.TenantId, invoice, await _numbering.NextAsync(OperationsNumberKind.Payment, cancellationToken), request.Method, request.Amount,
                request.Reason, request.Method == OperationsPaymentMethod.Card ? null : request.Reference, drawer, access.User.Id, key, utcNow);
        }
        catch (DomainException exception)
        {
            return RefundOperationsPaymentResponse.Failure(exception.Error);
        }

        // Slice 5F step 4 (review): a card refund is kept first, pending, so the amount is reserved and never given back twice; then Stripe gives the card
        // back (its idempotency key is this refund) and settles it. Stripe refusing: the refund is taken off again. Stripe silent: it stays pending and the cashier's next try sends it again.
        if (request.Method == OperationsPaymentMethod.Card)
        {
            return await CardAsync(access.User.Id, invoice, refund, cancellationToken);
        }

        _payments.Add(refund);
        _audit.Record(access.User.Id, "ops.payments.refunded", nameof(OperationsRefund), refund.Id,
            after: new { invoice = invoice.Number, number = refund.Number, method = refund.Method.ToString(), refund.Amount, refund.Reason });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RefundOperationsPaymentResponse.Success(refund.Id);
        }

        _unitOfWork.DiscardChanges();
        return await _payments.FindRefundByKeyAsync(key, cancellationToken) is { } made
            ? RefundOperationsPaymentResponse.Success(made.Id)
            : RefundOperationsPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private async Task<RefundOperationsPaymentResponse> CardAsync(Guid by, OperationsInvoice invoice, OperationsRefund refund, CancellationToken cancellationToken)
    {
        refund.AwaitCard();
        _payments.Add(refund);
        _audit.Record(by, "ops.payments.card_refund_started", nameof(OperationsRefund), refund.Id,
            after: new { invoice = invoice.Number, number = refund.Number, refund.Amount, refund.Reason });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return RefundOperationsPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return await SendCardAsync(by, invoice, refund, cancellationToken);
    }

    // Asks Stripe to give the card back (once, by the refund's own key) and settles the pending refund. A failed save leaves it pending: sent again, Stripe
    // returns the same refund, so the next try settles it.
    private async Task<RefundOperationsPaymentResponse> SendCardAsync(Guid by, OperationsInvoice invoice, OperationsRefund refund, CancellationToken cancellationToken)
    {
        (string? reference, Error? refused) = await CardRefundAsync(invoice, refund.Amount, refund.Id, cancellationToken);

        if (refused == OperationsCardErrors.ProviderUnavailable)
        {
            return RefundOperationsPaymentResponse.Failure(refused);
        }

        if (refused is not null)
        {
            refund.CardRefused(invoice, _timeProvider.GetUtcNow());
            _payments.Remove(refund);
            _audit.Record(by, "ops.payments.card_refund_refused", nameof(OperationsRefund), refund.Id, after: new { invoice = invoice.Number, refund.Amount });

            // Not saved (the invoice changed meanwhile): it stays pending and the next try asks Stripe again, which refuses again.
            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                _unitOfWork.DiscardChanges();
            }

            return RefundOperationsPaymentResponse.Failure(refused);
        }

        refund.ByCard(reference!);
        _audit.Record(by, "ops.payments.refunded", nameof(OperationsRefund), refund.Id,
            after: new { invoice = invoice.Number, number = refund.Number, method = refund.Method.ToString(), refund.Amount, refund.Reason, stripe = reference });

        // Given back at Stripe: kept pending here if this save fails; sent again, Stripe returns this same refund and it is settled.
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return RefundOperationsPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return RefundOperationsPaymentResponse.Success(refund.Id);
    }

    private async Task<(string? Reference, Error? Refused)> CardRefundAsync(OperationsInvoice invoice, decimal amount, Guid key, CancellationToken cancellationToken)
    {
        string? intent = (await _payments.ListInvoicePaymentsAsync(invoice.Id, cancellationToken)).OrderByDescending(payment => payment.TakenUtc)
            .SelectMany(payment => payment.Tenders).FirstOrDefault(tender => tender.Method == OperationsPaymentMethod.Card && tender.Amount >= amount)?.Reference;

        if (intent is null || await _cards.SecretKeyAsync(cancellationToken) is not { } secret)
        {
            return (null, OperationsCardErrors.RefundNotPossible);
        }

        try
        {
            return (await _cards.Gateway.RefundAsync(secret, intent, amount, invoice.CurrencyCode, key.ToString("D"), cancellationToken), null);
        }
        catch (OperationsCardRefusedException)
        {
            return (null, OperationsCardErrors.RefundNotPossible);
        }
        catch (OperationsCardUnavailableException)
        {
            return (null, OperationsCardErrors.ProviderUnavailable);
        }
    }
}
