using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBill.cs
//
// PURPOSE:
// A bill the company received (Slice 8, payables): from a carrier, a partner agent or another vendor.
//
// RESPONSIBILITIES:
// - Keep who sent it, their reference, what it is for, its dates and amount, and the shares put on dispatches or shipments
// - Entered by one person, approved by another (or at once when the company does not ask for approval), rejected with a reason
// - Record what was paid on it, up to its amount; paid in full when it is
//
// IMPORTANT NOTES:
// - Only an approved (or paid) bill counts in the costs; a rejected one can be corrected and approved later
// - Changed only while entered or rejected; voided only while nothing is paid
// - One currency: the company's
// =========================================================
public sealed class OperationsBill : OperationsEntity
{
    public const int ReferenceMaxLength = 60;

    public const int NoteMaxLength = 500;

    public const int MaxAllocations = 50;

    private readonly List<OperationsBillAllocation> _allocations = new();

    private readonly List<OperationsBillPayment> _payments = new();

    // Review: everyone who entered or changed the bill; none of them may approve it while the company asks for approval.
    private readonly List<Guid> _editors = new();

    private OperationsBill()
    {
        PartyName = string.Empty;
        Reference = string.Empty;
        CurrencyCode = string.Empty;
    }

    private OperationsBill(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        PartyName = string.Empty;
        Reference = string.Empty;
        CurrencyCode = string.Empty;
    }

    public OperationsBillParty Party { get; private set; }

    public Guid? CarrierId { get; private set; }

    // A partner agent is a partner-operated warehouse (Slice 7 step 4).
    public Guid? PartnerWarehouseId { get; private set; }

    public Guid? VendorId { get; private set; }

    // Who sent it, as it read when entered.
    public string PartyName { get; private set; }

    // Their invoice number.
    public string Reference { get; private set; }

    public OperationsBillCategory Category { get; private set; }

    public DateOnly BilledOn { get; private set; }

    public DateOnly DueOn { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public string? Note { get; private set; }

    public OperationsBillStatus Status { get; private set; }

    public Guid EnteredByUserId { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? RejectReason { get; private set; }

    public string? VoidReason { get; private set; }

    public IReadOnlyList<Guid> Editors => _editors;

    public decimal PaidTotal { get; private set; }

    public IReadOnlyList<OperationsBillAllocation> Allocations => _allocations;

    public IReadOnlyList<OperationsBillPayment> Payments => _payments;

    public decimal Allocated => _allocations.Sum(allocation => allocation.Amount);

    public decimal Owed => Status is OperationsBillStatus.Approved or OperationsBillStatus.Paid ? Amount - PaidTotal : 0m;

    // Counted in the costs.
    public bool Counts => Status is OperationsBillStatus.Approved or OperationsBillStatus.Paid;

    public static OperationsBill Enter(Guid tenantId, OperationsBillParty party, Guid partyId, string partyName, string? reference, OperationsBillCategory category,
        DateOnly billedOn, DateOnly dueOn, decimal amount, string currencyCode, string? note, IReadOnlyList<OperationsBillAllocation> allocations, Guid enteredBy, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        OperationsBill bill = new(tenantId, utcNow) { Status = OperationsBillStatus.Entered, EnteredByUserId = enteredBy, CurrencyCode = currencyCode };
        bill.Fill(party, partyId, partyName, reference, category, billedOn, dueOn, amount, note, allocations);
        bill._editors.Add(enteredBy);
        return bill;
    }

    // A correction before approval (or after a rejection, which sends it back to be approved).
    public void Change(OperationsBillParty party, Guid partyId, string partyName, string? reference, OperationsBillCategory category, DateOnly billedOn, DateOnly dueOn,
        decimal amount, string? note, IReadOnlyList<OperationsBillAllocation> allocations, Guid by, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsBillStatus.Entered or OperationsBillStatus.Rejected))
        {
            throw new DomainException(OperationsPayableErrors.NotEditable);
        }

        Fill(party, partyId, partyName, reference, category, billedOn, dueOn, amount, note, allocations);
        (Status, DecidedByUserId, DecidedUtc, RejectReason) = (OperationsBillStatus.Entered, null, null, null);

        if (!_editors.Contains(by))
        {
            _editors.Add(by);
        }

        MarkAsModified(utcNow);
    }

    // Approved by someone other than who entered it; the company may approve its own bills at once (approval turned off).
    public void Approve(Guid by, bool selfAllowed, DateTimeOffset utcNow)
    {
        if (Status is not OperationsBillStatus.Entered)
        {
            throw new DomainException(OperationsPayableErrors.NotApprovable);
        }

        if ((by == EnteredByUserId || _editors.Contains(by)) && !selfAllowed)
        {
            throw new DomainException(OperationsPayableErrors.OwnBill);
        }

        (Status, DecidedByUserId, DecidedUtc, RejectReason) = (OperationsBillStatus.Approved, by, utcNow, null);
        MarkAsModified(utcNow);
    }

    public void Reject(Guid by, string? reason, DateTimeOffset utcNow)
    {
        if (Status is not OperationsBillStatus.Entered)
        {
            throw new DomainException(OperationsPayableErrors.NotApprovable);
        }

        if (by == EnteredByUserId || _editors.Contains(by))
        {
            throw new DomainException(OperationsPayableErrors.OwnBill);
        }

        RejectReason = OperationsPayableText.Required(reason, NoteMaxLength, OperationsPayableErrors.ReasonRequired);
        (Status, DecidedByUserId, DecidedUtc) = (OperationsBillStatus.Rejected, by, utcNow);
        MarkAsModified(utcNow);
    }

    // Paid on a real day: not before the year 2000 and not after today (the company's day).
    public OperationsBillPayment Pay(DateOnly paidOn, decimal amount, OperationsBillPaymentMethod method, string? reference, Guid by, DateOnly today, DateTimeOffset utcNow)
    {
        if (Status is not OperationsBillStatus.Approved)
        {
            throw new DomainException(OperationsPayableErrors.NotPayable);
        }

        if (paidOn.Year < 2000 || paidOn > today)
        {
            throw new DomainException(OperationsPayableErrors.DatesInvalid);
        }

        OperationsBillPayment payment = new(paidOn, amount, method, reference, by, utcNow);

        if (payment.Amount > Amount - PaidTotal)
        {
            throw new DomainException(OperationsPayableErrors.PaymentTooHigh);
        }

        _payments.Add(payment);
        PaidTotal += payment.Amount;

        if (PaidTotal == Amount)
        {
            Status = OperationsBillStatus.Paid;
        }

        MarkAsModified(utcNow);
        return payment;
    }

    public void Void(string? reason, Guid by, DateTimeOffset utcNow)
    {
        if (Status is OperationsBillStatus.Voided or OperationsBillStatus.Paid || PaidTotal > 0m)
        {
            throw new DomainException(OperationsPayableErrors.NotVoidable);
        }

        VoidReason = OperationsPayableText.Required(reason, NoteMaxLength, OperationsPayableErrors.ReasonRequired);
        (Status, DecidedByUserId, DecidedUtc) = (OperationsBillStatus.Voided, by, utcNow);
        MarkAsModified(utcNow);
    }

    private void Fill(OperationsBillParty party, Guid partyId, string partyName, string? reference, OperationsBillCategory category, DateOnly billedOn, DateOnly dueOn,
        decimal amount, string? note, IReadOnlyList<OperationsBillAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(allocations);

        if (!Enum.IsDefined(party) || partyId == Guid.Empty || string.IsNullOrWhiteSpace(partyName))
        {
            throw new DomainException(OperationsPayableErrors.PartyRequired);
        }

        if (!Enum.IsDefined(category))
        {
            throw new DomainException(OperationsPayableErrors.TextInvalid);
        }

        if (dueOn < billedOn || billedOn.Year < 2000 || dueOn.Year > 2200)
        {
            throw new DomainException(OperationsPayableErrors.DatesInvalid);
        }

        decimal total = OperationsPayableText.Money(amount, OperationsPayableErrors.AmountInvalid);

        if (allocations.Count > MaxAllocations || allocations.Sum(allocation => allocation.Amount) > total)
        {
            throw new DomainException(OperationsPayableErrors.AllocationTooHigh);
        }

        if (allocations.GroupBy(allocation => (allocation.DispatchId, allocation.ShipmentId)).Any(group => group.Count() > 1))
        {
            throw new DomainException(OperationsPayableErrors.AllocationInvalid);
        }

        // Review: a dispatch carries only its carriers' bills (they replace its estimate); a carrier's bill goes on dispatches, never on a shipment already
        // carrying its share of a dispatch.
        if (allocations.Any(allocation => (allocation.DispatchId is not null) != (party == OperationsBillParty.Carrier)))
        {
            throw new DomainException(OperationsPayableErrors.ShareNotAllowed);
        }

        (Party, CarrierId, PartnerWarehouseId, VendorId) = (party, party == OperationsBillParty.Carrier ? partyId : null, party == OperationsBillParty.Partner ? partyId : null,
            party == OperationsBillParty.Vendor ? partyId : null);
        PartyName = partyName.Trim();
        Reference = OperationsPayableText.Required(reference, ReferenceMaxLength, OperationsPayableErrors.ReferenceRequired);
        (Category, BilledOn, DueOn, Amount) = (category, billedOn, dueOn, total);
        Note = OperationsPayableText.Optional(note, NoteMaxLength);
        _allocations.Clear();
        _allocations.AddRange(allocations);
    }
}
