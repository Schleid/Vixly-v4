using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Cards;

// =========================================================
// FILE: OperationsCardDesk.cs
//
// PURPOSE:
// What every card use case shares (Slice 5F step 4): the company's Stripe keys, starting a charge, and recording it once Stripe says it is paid.
//
// RESPONSIBILITIES:
// - Read the company's keys (protected with the server's key)
// - Start a charge on Stripe's payment page or a card reader
// - Check a charge against Stripe and record the payment, once
//
// IMPORTANT NOTES:
// - Paid only on Stripe's word: its webhook (signature checked) or a read of the payment from Stripe
// - Recording twice is impossible: a charge is paid once (its status), and the payment's request key is the charge's id
// - When the invoice no longer owes exactly what Stripe took, nothing is applied and staff refund it (PaidNotApplied)
// =========================================================
public sealed class OperationsCardDesk
{
    private readonly IOperationsCardRepository _cards;

    private readonly IOperationsCardGateway _gateway;

    private readonly IOperationsSecretProtector _protector;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsCardDesk(IOperationsCardRepository cards, IOperationsCardGateway gateway, IOperationsSecretProtector protector, IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments, OperationsNumbering numbering, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IOperationsCardGateway Gateway => _gateway;

    public IOperationsCardRepository Charges => _cards;

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    // The company's secret key, or none when no account is connected (or the key cannot be read back).
    public async Task<string?> SecretKeyAsync(CancellationToken cancellationToken) =>
        await _cards.GetProviderAsync(cancellationToken) is { } provider ? _protector.Unprotect(provider.SecretKeyProtected) : null;

    public async Task<string?> WebhookSecretAsync(CancellationToken cancellationToken) =>
        await _cards.GetProviderAsync(cancellationToken) is { } provider ? _protector.Unprotect(provider.WebhookSecretProtected) : null;

    // Cards are offered when the company accepts them and its account is connected.
    public static bool Offered(OperationsTenantSettings? settings, bool connected) => connected && settings?.Accepts(OperationsPaymentMethod.Card) == true;

    // A new charge for what the invoice owes; the one already started for it is cancelled first (on its reader too).
    public async Task<(OperationsCardCharge? Charge, string? PageUrl, Shared.Kernel.Errors.Error? Refused)> StartAsync(string secretKey, OperationsInvoice invoice,
        OperationsCardChannel channel, OperationsCardReader? reader, Guid? startedBy, string? email, Func<Guid, string> returnUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(returnUrl);
        DateTimeOffset utcNow = UtcNow;

        // One at a time per invoice: the one started before is expired at Stripe first. The portal never takes over a charge at the counter (review).
        if (await _cards.FindStartedAsync(invoice.Id, cancellationToken) is { } earlier)
        {
            if (channel == OperationsCardChannel.Portal && earlier.Channel != OperationsCardChannel.Portal)
            {
                return (null, null, OperationsCardErrors.AlreadyStarted);
            }

            if (!await CancelAsync(secretKey, earlier, cancellationToken))
            {
                return (null, null, earlier.Status == OperationsCardChargeStatus.Started ? OperationsCardErrors.ProviderUnavailable : OperationsPaymentErrors.NothingOwed);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        OperationsCardCharge charge;

        try
        {
            charge = OperationsCardCharge.Start(invoice.TenantId, invoice, channel, startedBy, reader?.Id, OperationsCardProvider.IsLive(secretKey), utcNow);
        }
        catch (DomainException exception)
        {
            return (null, null, exception.Error);
        }

        _cards.Add(charge);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return (null, null, OperationsCardErrors.AlreadyStarted);
        }

        OperationsCardPageRequest ask = new(charge.Id, charge.Amount, charge.CurrencyCode, invoice.Number, email, channel == OperationsCardChannel.Reader ? null : returnUrl(charge.Id));
        string? pageUrl = null;

        try
        {
            if (reader is not null)
            {
                charge.Sent(null, await _gateway.StartReaderAsync(secretKey, reader.StripeReaderId, ask, cancellationToken), null, utcNow);
            }
            else
            {
                OperationsCardPage page = await _gateway.CreatePageAsync(secretKey, ask, cancellationToken);
                charge.Sent(page.CheckoutSessionId, null, page.Url, utcNow);
                pageUrl = page.Url;
            }
        }
        catch (Exception exception) when (exception is OperationsCardRefusedException or OperationsCardUnavailableException or DomainException)
        {
            charge.Cancel(utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (null, null, exception is OperationsCardUnavailableException ? OperationsCardErrors.ProviderUnavailable : OperationsCardErrors.ProviderRefused);
        }

        _audit.Record(startedBy, "ops.payments.card_started", nameof(OperationsCardCharge), charge.Id,
            after: new { invoice = invoice.Number, charge.Amount, channel = channel.ToString(), reader = reader?.Label });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (charge, pageUrl, null);
    }

    // Cancelled only once Stripe can no longer take the money (review): the page is expired, the payment cancelled. Stripe refusing because it is
    // already paid records the payment instead. Returns whether the charge is now closed (cancelled or paid); false when Stripe could not confirm it.
    public async Task<bool> CancelAsync(string? secretKey, OperationsCardCharge charge, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(charge);

        if (charge.Status != OperationsCardChargeStatus.Started)
        {
            return true;
        }

        if (secretKey is null)
        {
            return false;
        }

        try
        {
            if (charge.ReaderId is { } readerId && await _cards.GetReaderAsync(readerId, cancellationToken) is { } reader)
            {
                try
                {
                    await _gateway.CancelReaderAsync(secretKey, reader.StripeReaderId, cancellationToken);
                }
                catch (OperationsCardRefusedException)
                {
                    // The reader may already be idle; the payment itself is cancelled next.
                }
            }

            if (charge.CheckoutSessionId is { } session)
            {
                await _gateway.ExpirePageAsync(secretKey, session, cancellationToken);
            }
            else if (charge.PaymentIntentId is { } intent)
            {
                await _gateway.CancelPaymentAsync(secretKey, intent, cancellationToken);
            }
        }
        catch (OperationsCardRefusedException)
        {
            // Stripe would not expire or cancel it: already paid, already over, or still processing. Ask; closed only when Stripe says it is paid or over
            // (second review: never cancelled here on a guess, or a late payment would land on nothing).
            await RefreshAsync(secretKey, charge, cancellationToken);
            return charge.Status != OperationsCardChargeStatus.Started;
        }
        catch (OperationsCardUnavailableException)
        {
            return false;
        }

        charge.Cancel(UtcNow);
        return true;
    }

    // Review: before the account is disconnected or replaced, every charge Stripe could still take is closed there (or recorded when already paid),
    // so no payment lands on an account the company no longer reads. Returns false when one could not be closed; nothing is then disconnected.
    public async Task<bool> CloseOpenAsync(string? secretKey, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsCardCharge> open = await _cards.ListStartedAsync(cancellationToken);

        foreach (OperationsCardCharge charge in open)
        {
            // The old key cannot be read back (lost protection keys): Stripe cannot be asked with it, so the charge is closed here and recorded.
            if (secretKey is null)
            {
                charge.Cancel(UtcNow);
                _audit.Record(charge.StartedByUserId, "ops.payments.card_closed_unreachable", nameof(OperationsCardCharge), charge.Id, after: new { charge.Amount });
                continue;
            }

            if (!await CancelAsync(secretKey, charge, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    // Asks Stripe where a started charge stands and records it when paid. Returns the charge as it now stands.
    public async Task<OperationsCardCharge> RefreshAsync(string secretKey, OperationsCardCharge charge, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(charge);

        if (charge.Status != OperationsCardChargeStatus.Started)
        {
            return charge;
        }

        OperationsCardState state;

        try
        {
            state = charge.CheckoutSessionId is { } session
                ? await _gateway.GetPageAsync(secretKey, session, cancellationToken)
                : charge.PaymentIntentId is { } intent ? await _gateway.GetPaymentAsync(secretKey, intent, cancellationToken) : new OperationsCardState(false, false, null, 0m, null);
        }
        catch (Exception exception) when (exception is OperationsCardRefusedException or OperationsCardUnavailableException)
        {
            // Not known yet: Stripe's webhook, or the next check, settles it.
            return charge;
        }

        if (state.Paid)
        {
            _ = await RecordPaidAsync(charge, state, cancellationToken);
        }
        else if (state.Over)
        {
            charge.Cancel(UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return charge;
    }

    // Stripe says it is paid: the payment for the invoice when it still owes exactly that amount in that currency; otherwise kept for a refund.
    // A cancelled charge paid at the last moment is recorded too (review). Returns false when it could not be saved (Stripe sends its event again).
    public async Task<bool> RecordPaidAsync(OperationsCardCharge charge, OperationsCardState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(charge);
        ArgumentNullException.ThrowIfNull(state);

        if (charge.Status is not (OperationsCardChargeStatus.Started or OperationsCardChargeStatus.Cancelled) || state.PaymentIntentId is not { } intent)
        {
            return true;
        }

        // A test payment never settles a live charge, nor the reverse (review).
        if (state.Live is { } live && live != charge.Live)
        {
            _audit.Record(charge.StartedByUserId, "ops.payments.card_mode_mismatch", nameof(OperationsCardCharge), charge.Id, after: new { live });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        DateTimeOffset utcNow = UtcNow;
        OperationsInvoice? invoice = await _billing.GetInvoiceAsync(charge.InvoiceId, cancellationToken);
        OperationsPayment? payment = null;

        if (invoice is not null && state.Amount == charge.Amount && string.Equals(state.CurrencyCode, charge.CurrencyCode, StringComparison.OrdinalIgnoreCase) && invoice.Owed == state.Amount)
        {
            try
            {
                payment = OperationsPayment.TakeByCard(charge.TenantId, invoice, await _numbering.NextAsync(OperationsNumberKind.Payment, cancellationToken), state.Amount, intent,
                    charge.StartedByUserId, charge.Id, utcNow);
                _payments.Add(payment);
            }
            catch (DomainException)
            {
                payment = null;
            }
        }

        charge.PaidBy(payment?.Id, intent, utcNow);
        _audit.Record(charge.StartedByUserId, payment is null ? "ops.payments.card_not_applied" : "ops.payments.card_paid", nameof(OperationsCardCharge), charge.Id,
            after: new { invoice = invoice?.Number, payment = payment?.Number, state.Amount, currency = state.CurrencyCode, channel = charge.Channel.ToString() });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return true;
        }

        // The webhook and a check at the same instant (the other one recorded it), or the invoice changed meanwhile: Stripe's event comes again.
        _unitOfWork.DiscardChanges();
        return false;
    }
}
