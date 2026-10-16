using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformPayment.cs
//
// PURPOSE:
// Money received against one invoice, with its receipt number (D-B7, D-B11).
//
// RESPONSIBILITIES:
// - Hold amount, method, date received, reference, note and who recorded it
// - Move from Recorded to Reversed with a reason
//
// IMPORTANT NOTES:
// - Owned by PlatformInvoice: the invoice keeps its balance consistent
// - One payment pays one invoice; overpayment is refused
// =========================================================
public sealed class PlatformPayment : AuditableEntity
{
    private PlatformPayment()
    {
        ReceiptNumber = string.Empty;
    }

    internal PlatformPayment(
        Guid invoiceId,
        string receiptNumber,
        decimal amount,
        PlatformPaymentMethod method,
        DateTimeOffset receivedUtc,
        string? reference,
        string? note,
        Guid? recordedByUserId,
        DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformInvoiceId = invoiceId;
        ReceiptNumber = receiptNumber;
        Amount = amount;
        Method = method;
        ReceivedUtc = receivedUtc;
        Reference = reference;
        Note = note;
        RecordedByUserId = recordedByUserId;
        Status = PlatformPaymentStatus.Recorded;
    }

    public Guid PlatformInvoiceId { get; private set; }

    public string ReceiptNumber { get; private set; }

    public decimal Amount { get; private set; }

    public PlatformPaymentMethod Method { get; private set; }

    public DateTimeOffset ReceivedUtc { get; private set; }

    public string? Reference { get; private set; }

    public string? Note { get; private set; }

    public Guid? RecordedByUserId { get; private set; }

    public PlatformPaymentStatus Status { get; private set; }

    public Guid? ReversedByUserId { get; private set; }

    public DateTimeOffset? ReversedUtc { get; private set; }

    public string? ReversalReason { get; private set; }

    public bool Counts => Status == PlatformPaymentStatus.Recorded;

    // A null actor and reason mean the provider reversed it (a lost card dispute).
    internal void Reverse(Guid? actorUserId, string? reason, DateTimeOffset utcNow)
    {
        if (Status == PlatformPaymentStatus.Reversed)
        {
            throw new DomainException(PlatformBillingErrors.PaymentReversed);
        }

        Status = PlatformPaymentStatus.Reversed;
        ReversedByUserId = actorUserId;
        ReversedUtc = utcNow;
        ReversalReason = reason;
        MarkAsModified(utcNow);
    }
}
