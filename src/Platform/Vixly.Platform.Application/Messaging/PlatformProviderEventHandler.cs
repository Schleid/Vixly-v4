using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformProviderEventHandler.cs
//
// PURPOSE:
// Applies a verified card-provider event: payments, saved cards, failures, refunds and disputes (D-P5).
//
// RESPONSIBILITIES:
// - Find the transaction or link the event is about
// - Hand it to PlatformCardSettlement or PlatformPaymentsAdministration
// - Mark the stored event processed in the same commit
//
// IMPORTANT NOTES:
// - The only path that records a card payment (D-P5)
// - A failed commit throws so the outbox retries; a processed event is skipped
// =========================================================
public sealed class PlatformProviderEventHandler : IPlatformOutboxHandler
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformCardSettlement _settlement;

    public PlatformProviderEventHandler(PlatformPaymentsAdministration payments, PlatformCardSettlement settlement)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
    }

    public string MessageType => PlatformMessageTypes.ProviderEvent;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformProviderEventReceived received = PlatformOutboxWriter.Read<PlatformProviderEventReceived>(message);
        PlatformProviderEvent? record = await _payments.Store.GetEventAsync(received.ProviderEventRecordId, cancellationToken);

        if (record is null || record.ProcessedUtc is not null)
        {
            return;
        }

        string outcome = await ApplyAsync(received.Event, cancellationToken);
        record.MarkProcessed(outcome, _payments.UtcNow);

        if (!await _payments.SaveAsync(cancellationToken))
        {
            throw new DomainException(PlatformCommonErrors.ConcurrencyConflict);
        }
    }

    private async Task<string> ApplyAsync(PlatformCardEvent card, CancellationToken cancellationToken)
    {
        PlatformCardTransaction? transaction = await FindAsync(card, cancellationToken);

        switch (card.Kind)
        {
            case PlatformCardEventKind.CheckoutCompleted:
                return await CheckoutCompletedAsync(card, transaction, cancellationToken);
            case PlatformCardEventKind.CheckoutExpired:
                return transaction is not null && transaction.Cancel(_payments.UtcNow) ? "cancelled" : "no_change";
            case PlatformCardEventKind.PaymentSucceeded:
                // A paid checkout names the card only here, not on checkout completion: save it now when asked.
                return transaction is null
                    ? "unknown_transaction"
                    : await _settlement.ChargeSucceededAsync(transaction, card.PaymentId, cancellationToken) + await SaveCardIfAskedAsync(card, cancellationToken);
            case PlatformCardEventKind.PaymentFailed:
            case PlatformCardEventKind.PaymentRequiresAction:
                return await ProblemAsync(card, transaction, cancellationToken);
            case PlatformCardEventKind.RefundSucceeded:
                return transaction is null ? "unknown_transaction" : await _settlement.RefundSucceededAsync(transaction, card.RefundId, cancellationToken);
            case PlatformCardEventKind.RefundFailed:
                if (transaction is not null && transaction.Fail(card.RefundId, card.FailureCode, _payments.UtcNow))
                {
                    _payments.Audit(null, "platform.payments.card_refund_failed", null, nameof(PlatformInvoice), transaction.PlatformInvoiceId, new { code = transaction.FailureCode });
                    return "refund_failed";
                }

                return "no_change";
            case PlatformCardEventKind.DisputeOpened:
            case PlatformCardEventKind.DisputeClosed:
                return transaction is null
                    ? "unknown_transaction"
                    : await _settlement.DisputeAsync(transaction, card.Kind == PlatformCardEventKind.DisputeOpened ? null : card.DisputeWon, cancellationToken);
            default:
                return "ignored";
        }
    }

    private async Task<string> CheckoutCompletedAsync(PlatformCardEvent card, PlatformCardTransaction? transaction, CancellationToken cancellationToken)
    {
        string outcome = transaction is null ? "no_payment" : await _settlement.ChargeSucceededAsync(transaction, card.PaymentId, cancellationToken);
        outcome += await SaveCardIfAskedAsync(card, cancellationToken);

        if (PlatformCardMetadata.Id(card.Metadata, PlatformCardMetadata.Link) is Guid linkId
            && await _payments.Store.GetLinkByIdAsync(linkId, cancellationToken) is { Kind: PlatformPaymentLinkKind.SaveCard } link)
        {
            link.MarkUsed(_payments.UtcNow);
        }

        return outcome;
    }

    // Saves the card when the checkout asked for it and the event names one; "" otherwise. Saving twice is a no-op.
    private async Task<string> SaveCardIfAskedAsync(PlatformCardEvent card, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(card.PaymentMethodId)
            || !PlatformCardMetadata.Flag(card.Metadata, PlatformCardMetadata.SaveCard)
            || PlatformCardMetadata.Id(card.Metadata, PlatformCardMetadata.Tenant) is not Guid tenantId)
        {
            return string.Empty;
        }

        return "+" + await _settlement.SaveCardAsync(tenantId, card.PaymentMethodId, card.Metadata, cancellationToken);
    }

    private async Task<string> ProblemAsync(PlatformCardEvent card, PlatformCardTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is null || transaction.Kind != PlatformCardTransactionKind.Charge)
        {
            return "unknown_transaction";
        }

        // Inside a hosted checkout the tenant can still try another card; the checkout result decides.
        if (transaction.Origin is PlatformCardTransactionOrigin.PayLink or PlatformCardTransactionOrigin.Registration)
        {
            return "checkout_retry";
        }

        PlatformInvoice invoice = await _payments.Billing.Invoices.GetAsync(transaction.PlatformInvoiceId, cancellationToken)
            ?? throw new DomainException(PlatformBillingErrors.InvoiceNotFound);
        PlatformCardOutcome outcome = card.Kind == PlatformCardEventKind.PaymentRequiresAction ? PlatformCardOutcome.RequiresAction : PlatformCardOutcome.Failed;
        PlatformCardTransactionStatus before = transaction.Status;
        _payments.ApplyChargeResult(transaction, invoice, new PlatformCardResult(card.PaymentId, outcome, card.FailureCode));
        return before == transaction.Status ? "no_change" : outcome == PlatformCardOutcome.Failed ? "failed" : "requires_action";
    }

    private async Task<PlatformCardTransaction?> FindAsync(PlatformCardEvent card, CancellationToken cancellationToken)
    {
        if (PlatformCardMetadata.Id(card.Metadata, PlatformCardMetadata.Transaction) is Guid transactionId
            && await _payments.Store.GetTransactionAsync(transactionId, cancellationToken) is { } byId)
        {
            return byId;
        }

        return card.CheckoutId is null && card.PaymentId is null && card.RefundId is null
            ? null
            : await _payments.Store.FindTransactionAsync(card.CheckoutId, card.Kind is PlatformCardEventKind.RefundSucceeded or PlatformCardEventKind.RefundFailed ? null : card.PaymentId, card.RefundId, cancellationToken);
    }
}
