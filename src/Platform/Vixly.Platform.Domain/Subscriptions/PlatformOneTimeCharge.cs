using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformOneTimeCharge.cs
//
// PURPOSE:
// A one-off amount billed on the next invoice, such as training or data migration (D-S23).
//
// RESPONSIBILITIES:
// - Hold the description, amount, reason and author
// - Move from Pending to Billed or Cancelled
//
// IMPORTANT NOTES:
// - Owned by the PlatformSubscription aggregate
// - In the subscription's currency; before tax (D-S9)
// =========================================================
public sealed class PlatformOneTimeCharge : AuditableEntity
{
    private PlatformOneTimeCharge()
    {
        Description = string.Empty;
        Reason = string.Empty;
        CurrencyCode = string.Empty;
    }

    internal PlatformOneTimeCharge(
        Guid platformSubscriptionId,
        string description,
        decimal amount,
        string currencyCode,
        string reason,
        Guid? addedByUserId,
        DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformSubscriptionId = platformSubscriptionId;
        Description = description;
        Amount = amount;
        CurrencyCode = currencyCode;
        Reason = reason;
        AddedByUserId = addedByUserId;
        Status = PlatformOneTimeChargeStatus.Pending;
    }

    public Guid PlatformSubscriptionId { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public string Reason { get; private set; }

    public Guid? AddedByUserId { get; private set; }

    public PlatformOneTimeChargeStatus Status { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public DateTimeOffset? CancelledUtc { get; private set; }

    internal void Cancel(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status != PlatformOneTimeChargeStatus.Pending)
        {
            throw new DomainException(PlatformDiscountErrors.ChargeNotPending);
        }

        Status = PlatformOneTimeChargeStatus.Cancelled;
        CancelledByUserId = actorUserId;
        CancelledUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void MarkBilled(DateTimeOffset utcNow)
    {
        if (Status != PlatformOneTimeChargeStatus.Pending)
        {
            throw new DomainException(PlatformDiscountErrors.ChargeNotPending);
        }

        Status = PlatformOneTimeChargeStatus.Billed;
        MarkAsModified(utcNow);
    }
}
