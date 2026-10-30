using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditNote.cs
//
// PURPOSE:
// A credit note or a void of an invoice (Slice 5F, F-6 and I-6): requested by one person with a reason, approved or refused by another.
//
// RESPONSIBILITIES:
// - Request a credit of some lines (all or part) or a void, with its taxes worked out
// - Approve it (numbered, the invoice takes it back) or refuse it
//
// IMPORTANT NOTES:
// - Never more than a line still holds; taxes in proportion, the last credit of a kind takes what is left
// - A person never decides their own request
// - One request open per invoice (unique index)
// =========================================================
public sealed class OperationsCreditNote : OperationsEntity
{
    private readonly List<OperationsCreditNoteLine> _lines = new();

    private readonly List<OperationsCreditNoteTax> _taxes = new();

    private OperationsCreditNote()
    {
        Reason = string.Empty;
    }

    private OperationsCreditNote(Guid tenantId, OperationsInvoice invoice, OperationsCreditNoteKind kind, string reason, Guid requestedBy, Guid requestKey, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (InvoiceId, WarehouseId, Kind, Reason, RequestedByUserId, RequestedUtc, RequestKey, Status) =
            (invoice.Id, invoice.WarehouseId, kind, reason, requestedBy, utcNow, requestKey, OperationsCreditNoteStatus.Requested);
    }

    public Guid InvoiceId { get; private set; }

    // The invoice's warehouse: who may decide is checked against it.
    public Guid WarehouseId { get; private set; }

    public OperationsCreditNoteKind Kind { get; private set; }

    public OperationsCreditNoteStatus Status { get; private set; }

    // Given on approval (prefix CN by default).
    public string? Number { get; private set; }

    public string Reason { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public DateTimeOffset RequestedUtc { get; private set; }

    // The form that asked for it (ADR-0019): sent twice, one request.
    public Guid RequestKey { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? RefusalNote { get; private set; }

    public IReadOnlyList<OperationsCreditNoteLine> Lines => _lines;

    public IReadOnlyList<OperationsCreditNoteTax> Taxes => _taxes;

    public decimal Subtotal => _lines.Sum(line => line.Amount);

    public decimal TaxTotal => _taxes.Sum(tax => tax.Amount);

    // A void takes back the whole invoice.
    public decimal Total { get; private set; }

    public static OperationsCreditNote RequestVoid(Guid tenantId, OperationsInvoice invoice, string? reason, Guid requestedBy, Guid requestKey, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (!invoice.CanVoid)
        {
            throw new DomainException(OperationsBillingErrors.VoidNotPossible);
        }

        return new OperationsCreditNote(tenantId, invoice, OperationsCreditNoteKind.Void, Why(reason), requestedBy, requestKey, utcNow) { Total = invoice.Total };
    }

    // Some lines, each with the amount before tax to take back (all of what it still holds at most).
    public static OperationsCreditNote RequestCredit(Guid tenantId, OperationsInvoice invoice, IReadOnlyDictionary<Guid, decimal> amounts, string? reason, Guid requestedBy, Guid requestKey,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(amounts);
        invoice.EnsureIssued();
        OperationsCreditNote note = new(tenantId, invoice, OperationsCreditNoteKind.Credit, Why(reason), requestedBy, requestKey, utcNow);

        foreach ((Guid lineId, decimal amount) in amounts.Where(pair => pair.Value != 0m))
        {
            OperationsInvoiceLine line = invoice.Line(lineId);

            if (amount < 0m || decimal.Round(amount, 2) != amount || amount > line.Remaining)
            {
                throw new DomainException(OperationsBillingErrors.AmountInvalid);
            }

            note._lines.Add(new OperationsCreditNoteLine(lineId, amount));
        }

        if (note._lines.Count == 0)
        {
            throw new DomainException(OperationsBillingErrors.NothingToCredit);
        }

        note.Taxed(invoice);
        return note;
    }

    // The day it was approved, in the company's time zone, as the invoice's day (review of 5F).
    public DateOnly? DecidedOn { get; private set; }

    public void Approve(OperationsInvoice invoice, string number, Guid decidedBy, DateOnly decidedOn, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        Decide(decidedBy);

        if (invoice.Id != InvoiceId)
        {
            throw new DomainException(OperationsBillingErrors.CreditNotFound);
        }

        if (Kind == OperationsCreditNoteKind.Void)
        {
            invoice.Void(utcNow);
        }
        else
        {
            // What a line still holds may have shrunk since the request: checked again.
            if (_lines.Any(line => line.Amount > invoice.Line(line.InvoiceLineId).Remaining))
            {
                throw new DomainException(OperationsBillingErrors.AmountInvalid);
            }

            // Worked out again on the invoice as it is now: another credit may have been approved since the request (review of 5F).
            Taxed(invoice);
            invoice.Credit(_lines, _taxes, Total, utcNow);
        }

        (Status, Number, DecidedByUserId, DecidedUtc, DecidedOn) = (OperationsCreditNoteStatus.Approved, number, decidedBy, utcNow, decidedOn);
        MarkAsModified(utcNow);
    }

    public void Refuse(string? note, Guid decidedBy, DateTimeOffset utcNow)
    {
        Decide(decidedBy);
        (Status, RefusalNote, DecidedByUserId, DecidedUtc) =
            (OperationsCreditNoteStatus.Refused, OperationsBillingLimits.Text(note, OperationsBillingLimits.ReasonMaxLength, OperationsBillingErrors.TextInvalid), decidedBy, utcNow);
        MarkAsModified(utcNow);
    }

    // The taxes the lines give back: in proportion to the base credited; the last credit of a kind takes what is left, so they come back to the cent.
    private void Taxed(OperationsInvoice invoice)
    {
        _taxes.Clear();

        foreach (OperationsInvoiceTax tax in invoice.Taxes)
        {
            List<OperationsInvoiceLine> taxed = invoice.Lines.Where(line => line.Taxed && line.TaxKind == tax.Kind).ToList();
            decimal credited = _lines.Where(item => taxed.Any(line => line.Id == item.InvoiceLineId)).Sum(item => item.Amount);

            if (credited == 0m)
            {
                continue;
            }

            bool last = taxed.All(line => line.Remaining == _lines.Where(item => item.InvoiceLineId == line.Id).Sum(item => item.Amount));
            decimal left = tax.Amount - tax.Credited;
            _taxes.Add(new OperationsCreditNoteTax(tax.Kind, last ? left : Math.Min(left, OperationsBillingLimits.Money(tax.Amount * credited / tax.Basis))));
        }

        Total = Subtotal + TaxTotal;
    }

    private void Decide(Guid decidedBy)
    {
        if (Status != OperationsCreditNoteStatus.Requested)
        {
            throw new DomainException(OperationsBillingErrors.NotRequested);
        }

        if (decidedBy == RequestedByUserId)
        {
            throw new DomainException(OperationsBillingErrors.OwnRequest);
        }
    }

    private static string Why(string? reason) =>
        OperationsBillingLimits.Text(reason, OperationsBillingLimits.ReasonMaxLength, OperationsBillingErrors.TextInvalid) ?? throw new DomainException(OperationsBillingErrors.ReasonRequired);
}
