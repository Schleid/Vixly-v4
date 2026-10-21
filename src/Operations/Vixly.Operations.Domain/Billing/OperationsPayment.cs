using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsPayment.cs
//
// PURPOSE:
// A payment of an invoice at the cashier (Slice 5F, F-4, C-3, C-5 and C-6): one receipt, one or more tenders, paid in full.
//
// RESPONSIBILITIES:
// - Take a payment: the tenders must add up to what is owed, cash into an open drawer, Zelle after the customer accepted the terms
// - Record finance's check of a Zelle or transfer tender
//
// IMPORTANT NOTES:
// - Numbered with the payment prefix (PAY); the receipt shows every tender
// - Never more and never less than what is owed (F-4)
// =========================================================
public sealed class OperationsPayment : OperationsEntity
{
    public const int MaxTenders = 3;

    private readonly List<OperationsPaymentTender> _tenders = new();

    private OperationsPayment()
    {
        Number = string.Empty;
        CurrencyCode = string.Empty;
    }

    private OperationsPayment(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = string.Empty;
        CurrencyCode = string.Empty;
    }

    public string Number { get; private set; }

    public Guid InvoiceId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid WarehouseId { get; private set; }

    // The drawer its cash went into.
    public Guid? DrawerId { get; private set; }

    public Guid TakenByUserId { get; private set; }

    public DateTimeOffset TakenUtc { get; private set; }

    public string CurrencyCode { get; private set; }

    public decimal Total { get; private set; }

    // A tender still waits for finance (kept as a column for the list to verify).
    public bool Pending { get; private set; }

    // The customer accepted the company's Zelle terms before seeing its details (P-9).
    public bool ZelleTermsAccepted { get; private set; }

    // The form that took it (ADR-0019): sent twice, one payment.
    public Guid RequestKey { get; private set; }

    public IReadOnlyList<OperationsPaymentTender> Tenders => _tenders;

    public decimal Change => _tenders.Sum(tender => tender.Change ?? 0m);

    public decimal Cash => _tenders.Where(tender => tender.Method == OperationsPaymentMethod.Cash).Sum(tender => tender.Amount);

    public static OperationsPayment Take(Guid tenantId, OperationsInvoice invoice, string number, IReadOnlyList<OperationsTenderDraft> drafts, OperationsCashDrawer? drawer,
        bool zelleTermsAccepted, Guid takenBy, Guid requestKey, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(drafts);
        invoice.EnsureIssued();
        decimal owed = invoice.Owed;

        if (owed <= 0m)
        {
            throw new DomainException(OperationsPaymentErrors.NothingOwed);
        }

        List<OperationsTenderDraft> parts = drafts.Where(draft => draft.Amount != 0m).ToList();

        if (parts.Count is 0 or > MaxTenders || parts.Any(draft => !Enum.IsDefined(draft.Method) || draft.Amount < 0m || decimal.Round(draft.Amount, 2) != draft.Amount))
        {
            throw new DomainException(OperationsPaymentErrors.AmountInvalid);
        }

        if (parts.Sum(draft => draft.Amount) != owed)
        {
            throw new DomainException(OperationsPaymentErrors.NotInFull);
        }

        List<OperationsTenderDraft> cash = parts.Where(draft => draft.Method == OperationsPaymentMethod.Cash).ToList();

        if (cash.Count > 1)
        {
            throw new DomainException(OperationsPaymentErrors.OneCashTender);
        }

        if (cash.Count == 1)
        {
            if (drawer is not { Status: OperationsCashDrawerStatus.Open } || drawer.WarehouseId != invoice.WarehouseId || drawer.UserId != takenBy)
            {
                throw new DomainException(OperationsPaymentErrors.DrawerRequired);
            }

            if (cash[0].CashGiven is { } given && (given < cash[0].Amount || decimal.Round(given, 2) != given))
            {
                throw new DomainException(OperationsPaymentErrors.CashGivenTooLow);
            }
        }

        if (parts.Any(draft => draft.Method == OperationsPaymentMethod.Zelle) && !zelleTermsAccepted)
        {
            throw new DomainException(OperationsPaymentErrors.ZelleTermsRequired);
        }

        OperationsPayment payment = new(tenantId, utcNow)
        {
            Number = number,
            InvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            WarehouseId = invoice.WarehouseId,
            DrawerId = cash.Count == 1 ? drawer!.Id : null,
            TakenByUserId = takenBy,
            TakenUtc = utcNow,
            CurrencyCode = invoice.CurrencyCode,
            ZelleTermsAccepted = zelleTermsAccepted,
            RequestKey = requestKey
        };

        foreach (OperationsTenderDraft draft in parts)
        {
            payment._tenders.Add(new OperationsPaymentTender(draft.Method, draft.Amount,
                draft.Method == OperationsPaymentMethod.Cash ? draft.CashGiven : null,
                OperationsBillingLimits.Text(draft.Reference, 60, OperationsPaymentErrors.TextInvalid)));
        }

        payment.Total = owed;
        payment.Pending = payment._tenders.Any(tender => tender.Status == OperationsTenderStatus.AwaitingConfirmation);
        decimal pending = payment._tenders.Where(tender => tender.Status == OperationsTenderStatus.AwaitingConfirmation).Sum(tender => tender.Amount);
        invoice.Paid(owed - pending, pending, utcNow);
        drawer?.TakeIn(payment.Cash, utcNow);
        return payment;
    }

    // Finance checked the bank: received settles the tender; not received takes it back and the invoice is owed again (C-6).
    public OperationsPaymentTender Check(OperationsInvoice invoice, Guid tenderId, bool received, string? note, Guid byUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        OperationsPaymentTender tender = _tenders.FirstOrDefault(item => item.Id == tenderId) ?? throw new DomainException(OperationsPaymentErrors.NotFound);

        if (tender.Status != OperationsTenderStatus.AwaitingConfirmation || invoice.Id != InvoiceId)
        {
            throw new DomainException(OperationsPaymentErrors.NotAwaiting);
        }

        string? why = OperationsBillingLimits.Text(note, OperationsBillingLimits.ReasonMaxLength, OperationsPaymentErrors.TextInvalid);

        if (!received && why is null)
        {
            throw new DomainException(OperationsPaymentErrors.ReasonRequired);
        }

        tender.Checked(received, why, byUserId, utcNow);
        invoice.Confirmed(tender.Amount, received, utcNow);
        Pending = _tenders.Any(item => item.Status == OperationsTenderStatus.AwaitingConfirmation);
        MarkAsModified(utcNow);
        return tender;
    }
}
