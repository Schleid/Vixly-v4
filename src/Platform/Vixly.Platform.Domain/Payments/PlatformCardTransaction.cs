using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformCardTransaction.cs
//
// PURPOSE:
// One charge or refund sent to the card provider, and what the provider said about it (D-P5, D-P6).
//
// RESPONSIBILITIES:
// - Carry amount, currency, origin and the provider references
// - Move through Created, Pending, Requires action, Succeeded, Failed or Cancelled
// - Link the Slice 4 payment once a charge is applied; track disputes
//
// IMPORTANT NOTES:
// - The idempotency key is the transaction id, so a repeated request cannot charge twice
// - Succeeded is set only from a verified provider event or a provider lookup, never from a browser
// - Failure codes are short provider codes, never provider text (S-9)
// =========================================================
public sealed class PlatformCardTransaction : AuditableEntity
{
    private PlatformCardTransaction()
    {
        CurrencyCode = string.Empty;
        IdempotencyKey = string.Empty;
    }

    private PlatformCardTransaction(
        Guid tenantId,
        Guid invoiceId,
        PlatformCardTransactionKind kind,
        PlatformCardTransactionOrigin origin,
        PlatformCardProvider provider,
        decimal amount,
        string currencyCode,
        Guid? savedCardId,
        Guid? creditNoteId,
        Guid? chargeTransactionId,
        Guid? createdByUserId,
        DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        PlatformInvoiceId = invoiceId;
        Kind = kind;
        Origin = origin;
        Provider = provider;
        Amount = amount;
        CurrencyCode = currencyCode;
        SavedCardId = savedCardId;
        CreditNoteId = creditNoteId;
        ChargeTransactionId = chargeTransactionId;
        CreatedByUserId = createdByUserId;
        Status = PlatformCardTransactionStatus.Created;
        IdempotencyKey = "vx4-" + Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture);
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid PlatformInvoiceId { get; private set; }

    public PlatformCardTransactionKind Kind { get; private set; }

    public PlatformCardTransactionOrigin Origin { get; private set; }

    public PlatformCardProvider Provider { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public PlatformCardTransactionStatus Status { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string? ProviderCheckoutId { get; private set; }

    public string? ProviderPaymentId { get; private set; }

    public string? ProviderRefundId { get; private set; }

    public Guid? SavedCardId { get; private set; }

    public Guid? CreditNoteId { get; private set; }

    public Guid? ChargeTransactionId { get; private set; }

    public Guid? PlatformPaymentId { get; private set; }

    public string? FailureCode { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? CompletedUtc { get; private set; }

    public PlatformDisputeStatus? Dispute { get; private set; }

    public bool IsOpen => Status is PlatformCardTransactionStatus.Created or PlatformCardTransactionStatus.Pending or PlatformCardTransactionStatus.RequiresAction;

    public bool IsFinal => !IsOpen;

    public static PlatformCardTransaction Charge(Guid tenantId, Guid invoiceId, PlatformCardTransactionOrigin origin, PlatformCardProvider provider, decimal amount, string? currencyCode, Guid? savedCardId, Guid? createdByUserId, DateTimeOffset utcNow)
    {
        if (origin is PlatformCardTransactionOrigin.Refund or PlatformCardTransactionOrigin.ExcessRefund || !Enum.IsDefined(origin))
        {
            throw new DomainException(PlatformPaymentsErrors.AmountInvalid);
        }

        return New(tenantId, invoiceId, PlatformCardTransactionKind.Charge, origin, provider, amount, currencyCode, savedCardId, null, null, createdByUserId, utcNow);
    }

    public static PlatformCardTransaction Refund(PlatformCardTransaction charge, decimal amount, Guid? creditNoteId, PlatformCardTransactionOrigin origin, Guid? createdByUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(charge);

        if (charge.Kind != PlatformCardTransactionKind.Charge || charge.Status != PlatformCardTransactionStatus.Succeeded || charge.ProviderPaymentId is null
            || origin is not (PlatformCardTransactionOrigin.Refund or PlatformCardTransactionOrigin.ExcessRefund))
        {
            throw new DomainException(PlatformPaymentsErrors.RefundNotAllowed);
        }

        if (amount > charge.Amount)
        {
            throw new DomainException(PlatformPaymentsErrors.RefundAboveCharged);
        }

        return New(charge.PlatformTenantId, charge.PlatformInvoiceId, PlatformCardTransactionKind.Refund, origin, charge.Provider, amount, charge.CurrencyCode, null, creditNoteId, charge.Id, createdByUserId, utcNow);
    }

    private static PlatformCardTransaction New(Guid tenantId, Guid invoiceId, PlatformCardTransactionKind kind, PlatformCardTransactionOrigin origin, PlatformCardProvider provider, decimal amount, string? currencyCode, Guid? savedCardId, Guid? creditNoteId, Guid? chargeId, Guid? createdByUserId, DateTimeOffset utcNow)
    {
        string currency = PlatformPaymentsLimits.Currency(currencyCode);

        if (tenantId == Guid.Empty || invoiceId == Guid.Empty || amount <= 0m || decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(PlatformPaymentsErrors.AmountInvalid);
        }

        PlatformPaymentsLimits.ToMinorUnits(amount, currency);

        return new PlatformCardTransaction(tenantId, invoiceId, kind, origin, provider, amount, currency, savedCardId, creditNoteId, chargeId, createdByUserId, utcNow);
    }

    public long MinorUnits => PlatformPaymentsLimits.ToMinorUnits(Amount, CurrencyCode);

    public void AttachCheckout(string? checkoutId, DateTimeOffset utcNow)
    {
        RequireOpen();
        ProviderCheckoutId = PlatformPaymentsLimits.ProviderId(checkoutId);
        MarkAsModified(utcNow);
    }

    // The provider accepted the request and is still working on it.
    public bool MarkPending(string? providerId, DateTimeOffset utcNow)
    {
        return Move(PlatformCardTransactionStatus.Pending, providerId, null, utcNow);
    }

    public bool MarkRequiresAction(string? providerId, DateTimeOffset utcNow)
    {
        return Move(PlatformCardTransactionStatus.RequiresAction, providerId, null, utcNow);
    }

    // Returns false when it had already succeeded: a replayed event changes nothing.
    public bool Succeed(string? providerId, DateTimeOffset utcNow)
    {
        if (Status == PlatformCardTransactionStatus.Succeeded)
        {
            return false;
        }

        // A charge that failed once inside a checkout can still succeed with another card.
        if (Status == PlatformCardTransactionStatus.Cancelled)
        {
            throw new DomainException(PlatformPaymentsErrors.TransactionClosed);
        }

        SetReference(providerId);
        Status = PlatformCardTransactionStatus.Succeeded;
        FailureCode = null;
        CompletedUtc = utcNow;
        MarkAsModified(utcNow);
        return true;
    }

    public bool Fail(string? providerId, string? failureCode, DateTimeOffset utcNow)
    {
        return Move(PlatformCardTransactionStatus.Failed, providerId, PlatformPaymentsLimits.Code(failureCode ?? "unknown", PlatformPaymentsLimits.FailureCodeMaxLength), utcNow);
    }

    public bool Cancel(DateTimeOffset utcNow)
    {
        return Move(PlatformCardTransactionStatus.Cancelled, null, null, utcNow);
    }

    public void LinkPayment(Guid paymentId, DateTimeOffset utcNow)
    {
        if (Kind != PlatformCardTransactionKind.Charge || Status != PlatformCardTransactionStatus.Succeeded)
        {
            throw new DomainException(PlatformPaymentsErrors.TransactionClosed);
        }

        PlatformPaymentId = paymentId;
        MarkAsModified(utcNow);
    }

    public bool RecordDispute(PlatformDisputeStatus status, DateTimeOffset utcNow)
    {
        if (Kind != PlatformCardTransactionKind.Charge || Status != PlatformCardTransactionStatus.Succeeded || !Enum.IsDefined(status) || Dispute == status
            || Dispute is PlatformDisputeStatus.Won or PlatformDisputeStatus.Lost)
        {
            return false;
        }

        Dispute = status;
        MarkAsModified(utcNow);
        return true;
    }

    private bool Move(PlatformCardTransactionStatus status, string? providerId, string? failureCode, DateTimeOffset utcNow)
    {
        if (Status == status)
        {
            return false;
        }

        if (IsFinal && !(Status == PlatformCardTransactionStatus.Failed && status != PlatformCardTransactionStatus.Cancelled))
        {
            return false;
        }

        SetReference(providerId);
        Status = status;
        FailureCode = failureCode;
        CompletedUtc = IsFinal ? utcNow : null;
        MarkAsModified(utcNow);
        return true;
    }

    private void SetReference(string? providerId)
    {
        if (providerId is null)
        {
            return;
        }

        string reference = PlatformPaymentsLimits.ProviderId(providerId);

        if (Kind == PlatformCardTransactionKind.Refund)
        {
            ProviderRefundId = reference;
        }
        else
        {
            ProviderPaymentId = reference;
        }
    }

    private void RequireOpen()
    {
        if (!IsOpen)
        {
            throw new DomainException(PlatformPaymentsErrors.TransactionClosed);
        }
    }
}
