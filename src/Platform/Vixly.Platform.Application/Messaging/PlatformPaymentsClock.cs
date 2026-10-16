using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformPaymentsClock.cs
//
// PURPOSE:
// The card-payment work that runs on a schedule (D-P4, D-P5).
//
// RESPONSIBILITIES:
// - Charge the default card once, on or after the due date, for tenants who consented
// - Send refunds that are waiting
// - Ask the provider about transactions still open after 30 minutes
//
// IMPORTANT NOTES:
// - One invoice or transaction per commit: one tenant failing never stops another
// - Nothing runs while card payments are not ready on this host
// =========================================================
public sealed class PlatformPaymentsClock
{
    private const int BatchSize = 25;

    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformCardSettlement _settlement;

    private readonly IPlatformPaymentsQueries _queries;

    public PlatformPaymentsClock(PlatformPaymentsAdministration payments, PlatformCardSettlement settlement, IPlatformPaymentsQueries queries)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        if (!await _payments.ReadyAsync(cancellationToken))
        {
            return 0;
        }

        int handled = 0;
        DateTimeOffset utcNow = _payments.UtcNow;

        foreach (Guid invoiceId in await _queries.ListAutoChargeInvoicesAsync(utcNow, BatchSize, cancellationToken))
        {
            handled += await OneAsync(() => AutoChargeAsync(invoiceId, cancellationToken), cancellationToken);
        }

        foreach (Guid refundId in await _queries.ListUnsentRefundsAsync(BatchSize, cancellationToken))
        {
            handled += await OneAsync(() => SendRefundAsync(refundId, cancellationToken), cancellationToken);
        }

        foreach (Guid transactionId in await _queries.ListStaleTransactionsAsync(utcNow - PlatformPaymentsLimits.StaleAfter, BatchSize, cancellationToken))
        {
            handled += await OneAsync(() => ReconcileAsync(transactionId, cancellationToken), cancellationToken);
        }

        return handled;
    }

    // A refusal or provider error on one item is undone and left for the next run.
    private async Task<int> OneAsync(Func<Task<bool>> work, CancellationToken cancellationToken)
    {
        try
        {
            return await work() && await _payments.SaveAsync(cancellationToken) ? 1 : 0;
        }
        catch (DomainException)
        {
            _payments.UnitOfWork.DiscardChanges();
            return 0;
        }
        catch (PlatformCardGatewayException)
        {
            _payments.UnitOfWork.DiscardChanges();
            return 0;
        }
    }

    private async Task<bool> AutoChargeAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(invoiceId, cancellationToken);

        if (invoice is null || invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m
            || await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is not null
            || await _payments.Store.GetOpenChargeAsync(invoice.Id, cancellationToken) is not null
            || await _payments.Store.GetProfileAsync(invoice.PlatformTenantId, cancellationToken) is not { AutoCharge: true } profile)
        {
            return false;
        }

        PlatformSavedCard? card = profile.Matches(_payments.Gateway.Status.Provider, _payments.Gateway.Status.Mode ?? PlatformPaymentMode.Test)
            ? await _payments.DefaultCardAsync(invoice.PlatformTenantId, cancellationToken)
            : null;
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.Automatic, _payments.Gateway.Status.Provider, invoice.Balance, invoice.CurrencyCode, card?.Id, null, _payments.UtcNow);
        _payments.Store.AddTransaction(charge);

        // The one automatic attempt is recorded before the provider is asked (unique open charge).
        if (!await _payments.SaveAsync(cancellationToken))
        {
            return false;
        }

        if (card is null || card.IsExpiredAt(_payments.UtcNow))
        {
            _payments.ApplyChargeResult(charge, invoice, new PlatformCardResult(null, PlatformCardOutcome.Failed, card is null ? "no_saved_card" : "expired_card"));
            return true;
        }

        await _payments.ChargeSavedCardAsync(charge, profile, card, invoice, cancellationToken);
        return true;
    }

    private async Task<bool> SendRefundAsync(Guid refundId, CancellationToken cancellationToken)
    {
        if (await _payments.Store.GetTransactionAsync(refundId, cancellationToken) is not { Kind: PlatformCardTransactionKind.Refund, Status: PlatformCardTransactionStatus.Created } refund
            || refund.ChargeTransactionId is not Guid chargeId
            || await _payments.Store.GetTransactionAsync(chargeId, cancellationToken) is not { ProviderPaymentId: { } paymentId })
        {
            return false;
        }

        await SendRefundAsync(_payments, refund, paymentId, cancellationToken);
        return true;
    }

    public static async Task SendRefundAsync(PlatformPaymentsAdministration payments, PlatformCardTransaction refund, string paymentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payments);
        ArgumentNullException.ThrowIfNull(refund);

        PlatformCardResult result;

        try
        {
            result = await payments.Gateway.RefundAsync(
                new PlatformCardRefundRequest(paymentId, refund.MinorUnits, PlatformPaymentsAdministration.Metadata(refund.PlatformTenantId, refund.PlatformInvoiceId, refund.Id, null), refund.IdempotencyKey),
                cancellationToken);
        }
        catch (PlatformCardGatewayException exception) when (exception.OutcomeUnknown)
        {
            return;
        }
        catch (PlatformCardGatewayException exception)
        {
            result = new PlatformCardResult(null, PlatformCardOutcome.Failed, exception.Code);
        }

        if (result.Outcome is PlatformCardOutcome.Failed or PlatformCardOutcome.Cancelled)
        {
            refund.Fail(result.ProviderId, result.FailureCode, payments.UtcNow);
            payments.Audit(null, "platform.payments.card_refund_failed", null, nameof(PlatformInvoice), refund.PlatformInvoiceId, new { code = refund.FailureCode });
            return;
        }

        // Confirmed by the refund webhook.
        refund.MarkPending(result.ProviderId, payments.UtcNow);
    }

    private async Task<bool> ReconcileAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        if (await _payments.Store.GetTransactionAsync(transactionId, cancellationToken) is not { IsOpen: true, Kind: PlatformCardTransactionKind.Charge } charge)
        {
            return false;
        }

        DateTimeOffset utcNow = _payments.UtcNow;
        bool checkout = charge.Origin is PlatformCardTransactionOrigin.PayLink or PlatformCardTransactionOrigin.Registration;

        // A checkout that was never opened at the provider cannot be paid.
        if (checkout && charge.ProviderCheckoutId is null)
        {
            return charge.Cancel(utcNow);
        }

        PlatformInvoice invoice = await _payments.Billing.Invoices.GetAsync(charge.PlatformInvoiceId, cancellationToken)
            ?? throw new DomainException(PlatformBillingErrors.InvoiceNotFound);

        // A saved-card charge whose answer was lost: ask again with the same idempotency key.
        if (!checkout && charge.ProviderPaymentId is null)
        {
            if (charge.SavedCardId is Guid cardId
                && await _payments.Store.GetCardAsync(cardId, cancellationToken) is { } card
                && await _payments.Store.GetProfileAsync(charge.PlatformTenantId, cancellationToken) is { } profile)
            {
                await _payments.ChargeSavedCardAsync(charge, profile, card, invoice, cancellationToken);
            }

            charge.MarkAsModified(utcNow);
            return true;
        }

        PlatformCardResult result = await _payments.Gateway.LookupAsync(charge.ProviderCheckoutId, charge.ProviderPaymentId, cancellationToken);

        switch (result.Outcome)
        {
            case PlatformCardOutcome.Succeeded:
                await _settlement.ChargeSucceededAsync(charge, result.ProviderId, cancellationToken);
                break;
            case PlatformCardOutcome.Cancelled:
                charge.Cancel(utcNow);
                break;
            case PlatformCardOutcome.Failed when !checkout:
            case PlatformCardOutcome.RequiresAction when !checkout:
                _payments.ApplyChargeResult(charge, invoice, result);
                break;
            default:
                // Still open at the provider: look again in 30 minutes.
                charge.MarkAsModified(utcNow);
                break;
        }

        return true;
    }
}
