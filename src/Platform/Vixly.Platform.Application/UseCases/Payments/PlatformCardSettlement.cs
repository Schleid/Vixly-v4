using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformCardSettlement.cs
//
// PURPOSE:
// Applies what the card provider confirmed: payments, refunds, saved cards and disputes (D-P5, D-P6, D-P15).
//
// RESPONSIBILITIES:
// - Record a confirmed charge as a Card payment with its receipt, and refund any excess
// - Lower refund owed when a refund is confirmed
// - Save a card with its consent; record disputes
//
// IMPORTANT NOTES:
// - Every step is idempotent: a replayed event or a second lookup changes nothing twice
// - The caller saves
// =========================================================
public sealed class PlatformCardSettlement
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformPaymentApplier _applier;

    public PlatformCardSettlement(PlatformPaymentsAdministration payments, PlatformPaymentApplier applier)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _applier = applier ?? throw new ArgumentNullException(nameof(applier));
    }

    public async Task<string> ChargeSucceededAsync(PlatformCardTransaction charge, string? paymentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(charge);

        DateTimeOffset utcNow = _payments.UtcNow;

        if (charge.Kind != PlatformCardTransactionKind.Charge || !charge.Succeed(paymentId, utcNow))
        {
            return "duplicate";
        }

        PlatformInvoice invoice = await _payments.Billing.Invoices.GetAsync(charge.PlatformInvoiceId, cancellationToken)
            ?? throw new DomainException(PlatformBillingErrors.InvoiceNotFound);

        // The invoice may have been settled by hand meanwhile: apply what is due, refund the rest.
        decimal applied = invoice.Status == PlatformInvoiceStatus.Issued ? Math.Min(charge.Amount, invoice.Balance) : 0m;

        if (applied > 0m)
        {
            PlatformInvoicePaymentResult result = await _applier.ApplyAsync(null, invoice, applied, PlatformPaymentMethod.Card, utcNow, charge.ProviderPaymentId, null, null, cancellationToken);
            charge.LinkPayment(result.Payment.Id, utcNow);
        }

        decimal excess = charge.Amount - applied;

        if (excess > 0m)
        {
            _payments.Store.AddTransaction(PlatformCardTransaction.Refund(charge, excess, null, PlatformCardTransactionOrigin.ExcessRefund, null, utcNow));
            _payments.Audit(null, "platform.payments.excess_refund_started", null, nameof(PlatformInvoice), invoice.Id, new { number = invoice.Number, amount = PlatformPaymentsLimits.Format(excess), currency = charge.CurrencyCode });
        }

        _payments.Audit(null, "platform.payments.card_charge_succeeded", null, nameof(PlatformInvoice), invoice.Id,
            new { number = invoice.Number, origin = charge.Origin.ToString(), amount = PlatformPaymentsLimits.Format(charge.Amount), currency = charge.CurrencyCode });
        return excess > 0m ? "applied_with_excess" : "applied";
    }

    public async Task<string> RefundSucceededAsync(PlatformCardTransaction refund, string? refundId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refund);

        if (refund.Kind != PlatformCardTransactionKind.Refund || !refund.Succeed(refundId, _payments.UtcNow))
        {
            return "duplicate";
        }

        PlatformInvoice invoice = await _payments.Billing.Invoices.GetAsync(refund.PlatformInvoiceId, cancellationToken)
            ?? throw new DomainException(PlatformBillingErrors.InvoiceNotFound);

        // An excess refund gives back money that was never applied to the invoice.
        if (refund.Origin == PlatformCardTransactionOrigin.Refund)
        {
            _payments.Billing.Invoices.AddEvents(new[] { invoice.RecordCardRefund(refund.Amount, refund.ProviderRefundId, _payments.UtcNow) });
        }

        _payments.Audit(null, "platform.payments.card_refunded", null, nameof(PlatformInvoice), invoice.Id,
            new { number = invoice.Number, amount = PlatformPaymentsLimits.Format(refund.Amount), currency = refund.CurrencyCode, origin = refund.Origin.ToString() });
        return "refunded";
    }

    public async Task<string> SaveCardAsync(Guid tenantId, string? paymentMethodId, IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(paymentMethodId) || await _payments.Store.GetCardByProviderIdAsync(paymentMethodId, cancellationToken) is not null)
        {
            return "card_known";
        }

        PlatformTenantCardProfile? profile = await _payments.Store.GetProfileAsync(tenantId, cancellationToken);

        if (profile is null)
        {
            return "no_profile";
        }

        PlatformCardDetails details = await _payments.Gateway.GetCardAsync(paymentMethodId, cancellationToken);
        DateTimeOffset utcNow = _payments.UtcNow;
        PlatformCardAddedVia via = Enum.TryParse(metadata.GetValueOrDefault(PlatformCardMetadata.AddedVia), out PlatformCardAddedVia parsed) && Enum.IsDefined(parsed)
            ? parsed
            : PlatformCardAddedVia.PayLink;

        foreach (PlatformSavedCard other in await _payments.Store.ListCardsAsync(tenantId, cancellationToken))
        {
            other.SetDefault(false, utcNow);
        }

        PlatformSavedCard card = PlatformSavedCard.Create(tenantId, profile.Provider, details.PaymentMethodId, details.Brand, details.Last4, details.ExpiryMonth, details.ExpiryYear, via, utcNow);
        _payments.Store.AddCard(card);
        _payments.Audit(null, "platform.payments.card_saved", null, "PlatformTenant", tenantId, new { brand = card.Brand, last4 = card.Last4, via = via.ToString() });

        if (PlatformCardMetadata.Flag(metadata, PlatformCardMetadata.AutoCharge))
        {
            profile.GiveConsent(metadata.GetValueOrDefault(PlatformCardMetadata.ConsentEmail), metadata.GetValueOrDefault(PlatformCardMetadata.ConsentVersion), utcNow);
            _payments.Audit(null, "platform.payments.auto_charge_on", null, "PlatformTenant", tenantId, new { email = profile.ConsentEmail, version = profile.ConsentTextVersion });
        }

        return "card_saved";
    }

    public async Task<string> DisputeAsync(PlatformCardTransaction charge, bool? won, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(charge);

        PlatformDisputeStatus status = won switch
        {
            true => PlatformDisputeStatus.Won,
            false => PlatformDisputeStatus.Lost,
            _ => PlatformDisputeStatus.Open
        };

        if (!charge.RecordDispute(status, _payments.UtcNow))
        {
            return "duplicate";
        }

        PlatformInvoice invoice = await _payments.Billing.Invoices.GetAsync(charge.PlatformInvoiceId, cancellationToken)
            ?? throw new DomainException(PlatformBillingErrors.InvoiceNotFound);

        if (charge.PlatformPaymentId is Guid paymentId)
        {
            string code = status switch
            {
                PlatformDisputeStatus.Won => PlatformInvoiceEventCodes.DisputeWon,
                PlatformDisputeStatus.Lost => PlatformInvoiceEventCodes.DisputeLost,
                _ => PlatformInvoiceEventCodes.Disputed
            };
            _payments.Billing.Invoices.AddEvents(new[] { invoice.RecordDispute(paymentId, code, _payments.UtcNow) });
        }

        _payments.Audit(null, "platform.payments.dispute_" + status.ToString().ToLowerInvariant(), null, nameof(PlatformInvoice), invoice.Id, new { number = invoice.Number });
        return "dispute_" + status.ToString().ToLowerInvariant();
    }
}
