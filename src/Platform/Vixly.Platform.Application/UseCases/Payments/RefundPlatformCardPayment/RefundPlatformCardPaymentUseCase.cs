using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.RefundPlatformCardPayment;

// =========================================================
// FILE: RefundPlatformCardPaymentUseCase.cs
//
// PURPOSE:
// Refunds money to the card against a credit note the sales manager approved (D-P6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RefundPlatformCardPaymentUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public RefundPlatformCardPaymentUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<RefundPlatformCardPaymentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RefundPlatformCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingRefundCards, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RefundPlatformCardPaymentResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return RefundPlatformCardPaymentResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        IReadOnlyList<PlatformCardTransaction> transactions = await _payments.Store.ListForInvoiceAsync(invoice.Id, cancellationToken);
        PlatformRefundableCreditItem? credit = PlatformCardRefunds.Refundable(invoice, transactions).FirstOrDefault(item => item.CreditId == request.CreditId);

        if (credit is null)
        {
            return RefundPlatformCardPaymentResponse.Failure(PlatformPaymentsErrors.RefundNotAllowed);
        }

        if (request.Amount <= 0m || decimal.Round(request.Amount, 2) != request.Amount)
        {
            return RefundPlatformCardPaymentResponse.Failure(PlatformPaymentsErrors.AmountInvalid);
        }

        (PlatformCardTransaction? charge, decimal left) = PlatformCardRefunds.BestCharge(transactions);

        if (charge?.ProviderPaymentId is null || request.Amount > credit.Refundable || request.Amount > left)
        {
            return RefundPlatformCardPaymentResponse.Failure(PlatformPaymentsErrors.RefundAboveCharged);
        }

        PlatformCardTransaction refund = PlatformCardTransaction.Refund(charge, request.Amount, credit.CreditId, PlatformCardTransactionOrigin.Refund, context.PlatformUserId, _payments.UtcNow);
        _payments.Store.AddTransaction(refund);
        _payments.Audit(context, "platform.payments.card_refund_started", PlatformPermissions.BillingRefundCards, nameof(PlatformInvoice), invoice.Id,
            new { number = invoice.Number, credit = credit.Number, amount = PlatformPaymentsLimits.Format(refund.Amount), currency = refund.CurrencyCode });

        if (!await _payments.SaveAsync(cancellationToken))
        {
            return RefundPlatformCardPaymentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        // Sent now; if the provider cannot be reached the clock sends it with the same key.
        await Vixly.Platform.Application.Messaging.PlatformPaymentsClock.SendRefundAsync(_payments, refund, charge.ProviderPaymentId, cancellationToken);
        await _payments.SaveAsync(cancellationToken);
        return refund.Status == PlatformCardTransactionStatus.Failed
            ? RefundPlatformCardPaymentResponse.Failure(PlatformPaymentsErrors.ProviderUnavailable)
            : RefundPlatformCardPaymentResponse.Success();
    }
}
