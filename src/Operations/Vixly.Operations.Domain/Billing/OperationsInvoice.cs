using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoice.cs
//
// PURPOSE:
// An invoice (Slice 5F step 1, F-1 to F-6 and I-1 to I-7): issued once for a final shipment or a confirmed order of supplies, then never edited.
//
// RESPONSIBILITIES:
// - Issue it from a draft: number, parties, lines, taxes per kind and totals
// - Take back an approved credit note or void it
// - Say what is still owed
//
// IMPORTANT NOTES:
// - One per shipment and one per order (unique indexes)
// - Taxes: each kind on the total of its taxed lines, rounded to the cent (I-4)
// - Payments come with the cashier (step 2); PaidTotal stays 0 until then
// =========================================================
public sealed class OperationsInvoice : OperationsEntity
{
    private readonly List<OperationsInvoiceLine> _lines = new();

    private readonly List<OperationsInvoiceTax> _taxes = new();

    private OperationsInvoice()
    {
        Number = string.Empty;
        Reference = string.Empty;
        CurrencyCode = string.Empty;
        Language = string.Empty;
        Party = null!;
    }

    private OperationsInvoice(Guid tenantId, string number, OperationsInvoiceDraft draft, DateOnly issuedOn, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = number;
        (Kind, CustomerId, WarehouseId, ShipmentId, SupplySaleId) = (draft.Kind, draft.CustomerId, draft.WarehouseId, draft.ShipmentId, draft.SupplySaleId);
        (Reference, Route, ServiceCode, CurrencyCode, Language, Party) = (draft.Reference, draft.Route, draft.ServiceCode, draft.CurrencyCode, draft.Language, draft.Party);
        (TaxName, TaxPerKind, Note, PayAtDestination) = (draft.TaxName, draft.TaxPerKind, draft.Note, draft.PayAtDestination);
        (IssuedOn, IssuedUtc, Status) = (issuedOn, utcNow, OperationsInvoiceStatus.Issued);
        _lines.AddRange(draft.Lines);

        foreach (OperationsTaxKind kind in Enum.GetValues<OperationsTaxKind>())
        {
            decimal rate = draft.Rates.Of(kind);
            decimal basis = _lines.Where(line => line.Taxed && line.TaxKind == kind).Sum(line => line.Amount);

            if (rate > 0m && basis > 0m)
            {
                _taxes.Add(new OperationsInvoiceTax(kind, rate, basis, OperationsBillingLimits.Money(basis * rate / 100m)));
            }
        }

        (Gross, DiscountTotal, Subtotal) = (_lines.Sum(line => line.Gross), _lines.Sum(line => line.Discount), _lines.Sum(line => line.Amount));
        TaxTotal = _taxes.Sum(tax => tax.Amount);
        Total = Subtotal + TaxTotal;
    }

    public string Number { get; private set; }

    public OperationsInvoiceKind Kind { get; private set; }

    public Guid CustomerId { get; private set; }

    // The warehouse that issued it: the shipment's origin or the order's counter.
    public Guid WarehouseId { get; private set; }

    public Guid? ShipmentId { get; private set; }

    public Guid? SupplySaleId { get; private set; }

    // The shipment's or the order's number.
    public string Reference { get; private set; }

    public string? Route { get; private set; }

    public string? ServiceCode { get; private set; }

    public string CurrencyCode { get; private set; }

    // The customer's language at issue: the invoice is read and printed in it.
    public string Language { get; private set; }

    public OperationsInvoiceParty Party { get; private set; }

    public string? TaxName { get; private set; }

    public bool TaxPerKind { get; private set; }

    public string? Note { get; private set; }

    // Freight collect: to pay at the destination; the shipment moves unpaid (owner, 2026-10-21).
    public bool PayAtDestination { get; private set; }

    public DateOnly IssuedOn { get; private set; }

    public DateTimeOffset IssuedUtc { get; private set; }

    public OperationsInvoiceStatus Status { get; private set; }

    public DateTimeOffset? VoidedUtc { get; private set; }

    public IReadOnlyList<OperationsInvoiceLine> Lines => _lines;

    public IReadOnlyList<OperationsInvoiceTax> Taxes => _taxes;

    // Fixed at issue (kept as columns so lists can filter on them).
    public decimal Gross { get; private set; }

    public decimal DiscountTotal { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal Total { get; private set; }

    // Approved credit notes, tax included.
    public decimal CreditedTotal { get; private set; }

    // Slice 5F step 2: payments settled, payments waiting for finance (Zelle, transfer), and money given back.
    public decimal PaidTotal { get; private set; }

    public decimal PendingTotal { get; private set; }

    public decimal RefundedTotal { get; private set; }

    // What the customer still has to pay; below zero, what can be given back after a credit note.
    public decimal Owed => Status == OperationsInvoiceStatus.Voided ? 0m : Total - CreditedTotal - PaidTotal - PendingTotal + RefundedTotal;

    // At most what was credited beyond what was owed, and never more than was paid (C-8).
    public decimal Refundable => Owed < 0m ? Math.Min(-Owed, PaidTotal - RefundedTotal) : 0m;

    public bool CanVoid => Status == OperationsInvoiceStatus.Issued && CreditedTotal == 0m && PaidTotal == 0m && PendingTotal == 0m;

    public OperationsPaymentState PaymentState => Status == OperationsInvoiceStatus.Voided ? OperationsPaymentState.Voided
        : Owed > 0m ? OnAccount switch
        {
            OperationsAccountCharge.Charged => OperationsPaymentState.OnAccount,
            OperationsAccountCharge.AwaitingApproval => OperationsPaymentState.AwaitingCredit,
            _ => OperationsPaymentState.Unpaid
        }
        : PendingTotal > 0m ? OnAccount == OperationsAccountCharge.Charged ? OperationsPaymentState.OnAccount : OperationsPaymentState.AwaitingConfirmation
        : OperationsPaymentState.Paid;

    // Slice 5F step 2b (K-3, K-4): billed to the customer's credit account; charged, the shipment is released and the invoice stays owed until paid.
    public OperationsAccountCharge OnAccount { get; private set; }

    public DateOnly? DueOn { get; private set; }

    public Guid? AccountRequestedByUserId { get; private set; }

    public DateTimeOffset? AccountRequestedUtc { get; private set; }

    public Guid? AccountDecidedByUserId { get; private set; }

    // Released for bundling and dispatch (F-3): paid, or charged to the account.
    public bool Released => PaymentState is OperationsPaymentState.Paid or OperationsPaymentState.OnAccount;

    // The cashier bills the account: at once within what is available, or waiting for the credit department (K-3).
    public void ChargeToAccount(bool approved, DateOnly dueOn, Guid userId, DateTimeOffset utcNow)
    {
        EnsureIssued();

        if (OnAccount != OperationsAccountCharge.None)
        {
            throw new DomainException(OperationsCreditErrors.AlreadyOnAccount);
        }

        if (Owed <= 0m || PendingTotal > 0m)
        {
            throw new DomainException(OperationsCreditErrors.NothingOwed);
        }

        (OnAccount, DueOn, AccountRequestedByUserId, AccountRequestedUtc) = (approved ? OperationsAccountCharge.Charged : OperationsAccountCharge.AwaitingApproval, approved ? dueOn : null, userId, utcNow);
        AccountDecidedByUserId = approved ? userId : null;
        MarkAsModified(utcNow);
    }

    // The credit department decides a charge above the limit; never the person who asked for it.
    public void DecideCharge(bool approve, DateOnly dueOn, Guid userId, DateTimeOffset utcNow)
    {
        EnsureIssued();

        if (OnAccount != OperationsAccountCharge.AwaitingApproval)
        {
            throw new DomainException(OperationsCreditErrors.NotAwaiting);
        }

        // Paid meanwhile at the cashier (review): nothing is left to put on the account.
        if (Owed <= 0m)
        {
            throw new DomainException(OperationsCreditErrors.NothingOwed);
        }

        if (AccountRequestedByUserId == userId)
        {
            throw new DomainException(OperationsCreditErrors.OwnRequest);
        }

        (OnAccount, DueOn, AccountDecidedByUserId) = (approve ? OperationsAccountCharge.Charged : OperationsAccountCharge.None, approve ? dueOn : null, userId);
        MarkAsModified(utcNow);
    }

    internal void Paid(decimal settled, decimal pending, DateTimeOffset utcNow)
    {
        EnsureIssued();
        (PaidTotal, PendingTotal) = (PaidTotal + settled, PendingTotal + pending);
        MarkAsModified(utcNow);
    }

    // Finance checked a Zelle or transfer: received, it is paid; not received, it is owed again.
    internal void Confirmed(decimal amount, bool received, DateTimeOffset utcNow)
    {
        PendingTotal -= amount;

        if (received)
        {
            PaidTotal += amount;
        }

        MarkAsModified(utcNow);
    }

    internal void Refunded(decimal amount, DateTimeOffset utcNow)
    {
        RefundedTotal += amount;
        MarkAsModified(utcNow);
    }

    public static OperationsInvoice Issue(Guid tenantId, string number, OperationsInvoiceDraft draft, DateOnly issuedOn, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return draft.Lines.Count is 0 or > OperationsBillingLimits.MaxLines
            ? throw new DomainException(OperationsBillingErrors.NothingToInvoice)
            : new OperationsInvoice(tenantId, number, draft, issuedOn, utcNow);
    }

    public OperationsInvoiceLine Line(Guid lineId) => _lines.FirstOrDefault(line => line.Id == lineId) ?? throw new DomainException(OperationsBillingErrors.AmountInvalid);

    public void EnsureIssued()
    {
        if (Status != OperationsInvoiceStatus.Issued)
        {
            throw new DomainException(OperationsBillingErrors.Voided);
        }
    }

    // An approved credit note takes back its lines and its taxes.
    internal void Credit(IReadOnlyList<OperationsCreditNoteLine> lines, IReadOnlyList<OperationsCreditNoteTax> taxes, decimal total, DateTimeOffset utcNow)
    {
        EnsureIssued();

        foreach (OperationsCreditNoteLine line in lines)
        {
            Line(line.InvoiceLineId).Credit(line.Amount);
        }

        foreach (OperationsCreditNoteTax tax in taxes)
        {
            OperationsInvoiceTax held = _taxes.First(item => item.Kind == tax.Kind);

            // Never more than the tax still holds.
            if (tax.Amount < 0m || tax.Amount > held.Amount - held.Credited)
            {
                throw new DomainException(OperationsBillingErrors.AmountInvalid);
            }

            held.Credit(tax.Amount);
        }

        CreditedTotal += total;
        MarkAsModified(utcNow);
    }

    internal void Void(DateTimeOffset utcNow)
    {
        if (!CanVoid)
        {
            throw new DomainException(OperationsBillingErrors.VoidNotPossible);
        }

        (Status, VoidedUtc) = (OperationsInvoiceStatus.Voided, utcNow);
        MarkAsModified(utcNow);
    }
}
