using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformCreditNote.cs
//
// PURPOSE:
// A correction that lowers what an invoice asks for, or voids it (D-B6).
//
// RESPONSIBILITIES:
// - Hold the amount, its tax part, the reason, who asked and who decided
// - Move from Requested to Issued (numbered) or Rejected
//
// IMPORTANT NOTES:
// - Owned by PlatformInvoice
// - Four eyes: the requester never decides (sales manager approves)
// =========================================================
public sealed class PlatformCreditNote : AuditableEntity
{
    private PlatformCreditNote()
    {
        Reason = string.Empty;
    }

    internal PlatformCreditNote(Guid invoiceId, decimal amount, decimal taxPart, bool isVoid, string reason, Guid requestedByUserId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformInvoiceId = invoiceId;
        Amount = amount;
        TaxPart = taxPart;
        IsVoid = isVoid;
        Reason = reason;
        RequestedByUserId = requestedByUserId;
        Status = PlatformCreditNoteStatus.Requested;
    }

    public Guid PlatformInvoiceId { get; private set; }

    public string? Number { get; private set; }

    public decimal Amount { get; private set; }

    public decimal TaxPart { get; private set; }

    public bool IsVoid { get; private set; }

    public string Reason { get; private set; }

    public PlatformCreditNoteStatus Status { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? DecisionReason { get; private set; }

    public bool IsPending => Status == PlatformCreditNoteStatus.Requested;

    public bool IsIssued => Status == PlatformCreditNoteStatus.Issued;

    internal void Decide(Guid actorUserId, bool approve, string? number, string? reason, DateTimeOffset utcNow)
    {
        if (!IsPending)
        {
            throw new DomainException(PlatformBillingErrors.CreditNotPending);
        }

        if (actorUserId == RequestedByUserId)
        {
            throw new DomainException(PlatformBillingErrors.OwnRequest);
        }

        Status = approve ? PlatformCreditNoteStatus.Issued : PlatformCreditNoteStatus.Rejected;
        Number = approve ? number : null;
        DecidedByUserId = actorUserId;
        DecidedUtc = utcNow;
        DecisionReason = reason;
        MarkAsModified(utcNow);
    }
}
