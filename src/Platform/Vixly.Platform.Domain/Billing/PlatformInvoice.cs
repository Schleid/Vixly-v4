using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoice.cs
//
// PURPOSE:
// A numbered request for payment, built from what a subscription charges (D-B3).
//
// RESPONSIBILITIES:
// - Hold the lines, totals, customer snapshot, due date and language
// - Take payments up to the balance, reverse them, and take credit notes (D-B6, D-B7)
// - Say whether it is overdue and mark it past due once (D-B8)
//
// IMPORTANT NOTES:
// - Aggregate root: owns lines, payments and credit notes; protected by a row version
// - Never edited after issue: corrections are credit notes
// - Every change returns a history event for the same commit
// =========================================================
public sealed class PlatformInvoice : AuditableEntity
{
    private readonly List<PlatformInvoiceLine> _lines = new();
    private readonly List<PlatformPayment> _payments = new();
    private readonly List<PlatformCreditNote> _credits = new();

    private PlatformInvoice()
    {
        Number = string.Empty;
        SourceKey = string.Empty;
        CurrencyCode = string.Empty;
        Language = LanguageCode.Default;
        Customer = null!;
    }

    private PlatformInvoice(
        Guid platformTenantId,
        Guid? platformSubscriptionId,
        PlatformInvoiceKind kind,
        string number,
        string sourceKey,
        string currencyCode,
        LanguageCode language,
        PlatformInvoiceParty customer,
        DateTimeOffset dueUtc,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc,
        DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = platformTenantId;
        PlatformSubscriptionId = platformSubscriptionId;
        Kind = kind;
        Number = number;
        SourceKey = sourceKey;
        CurrencyCode = currencyCode;
        Language = language;
        Customer = customer;
        IssuedUtc = utcNow;
        DueUtc = dueUtc;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        Status = PlatformInvoiceStatus.Issued;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid? PlatformSubscriptionId { get; private set; }

    public PlatformInvoiceKind Kind { get; private set; }

    public string Number { get; private set; }

    public string SourceKey { get; private set; }

    public PlatformInvoiceStatus Status { get; private set; }

    public string CurrencyCode { get; private set; }

    public LanguageCode Language { get; private set; }

    public PlatformInvoiceParty Customer { get; private set; }

    public DateTimeOffset IssuedUtc { get; private set; }

    public DateTimeOffset DueUtc { get; private set; }

    public DateTimeOffset? PeriodStartUtc { get; private set; }

    public DateTimeOffset? PeriodEndUtc { get; private set; }

    public decimal NetTotal { get; private set; }

    public decimal DiscountTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal Total { get; private set; }

    public decimal PaidTotal { get; private set; }

    public decimal CreditedTotal { get; private set; }

    // Money given back to the card against credit notes (Slice 5, D-P6).
    public decimal RefundedTotal { get; private set; }

    public decimal Balance { get; private set; }

    public DateTimeOffset? PastDueUtc { get; private set; }

    public IReadOnlyList<PlatformInvoiceLine> Lines => _lines;

    public IReadOnlyList<PlatformPayment> Payments => _payments;

    public IReadOnlyList<PlatformCreditNote> Credits => _credits;

    public PlatformCreditNote? PendingCredit => _credits.FirstOrDefault(item => item.IsPending);

    // What was paid or credited beyond the total: owed back to the customer (refund: Slice 5 or by hand).
    public decimal RefundOwed => Math.Max(0m, PaidTotal + CreditedTotal - Total - RefundedTotal);

    public bool IsPartlyPaid => Status == PlatformInvoiceStatus.Issued && PaidTotal > 0m;

    // Past due starts the day after the due date (D-B8): nothing is overdue on its due day.
    public static DateTimeOffset OverdueCutoff(DateTimeOffset utcNow) => new(utcNow.UtcDateTime.Date, TimeSpan.Zero);

    public bool IsOverdueAt(DateTimeOffset utcNow) => Status == PlatformInvoiceStatus.Issued && DueUtc < OverdueCutoff(utcNow);

    // =====================================================
    // METHOD: ISSUE (D-B2, D-B3)
    // =====================================================
    public static PlatformInvoiceIssue Issue(
        Guid platformTenantId,
        Guid? platformSubscriptionId,
        PlatformInvoiceKind kind,
        string number,
        string sourceKey,
        string currencyCode,
        LanguageCode language,
        PlatformInvoiceParty customer,
        int paymentTermsDays,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc,
        IReadOnlyList<PlatformInvoiceLineInput> lines,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(lines);

        if (platformTenantId == Guid.Empty || !Enum.IsDefined(kind)
            || string.IsNullOrWhiteSpace(number) || number.Length > PlatformBillingLimits.NumberMaxLength
            || string.IsNullOrWhiteSpace(sourceKey) || sourceKey.Length > PlatformBillingLimits.SourceKeyMaxLength
            || paymentTermsDays < 0 || paymentTermsDays > PlatformBillingLimits.MaxPaymentTermsDays)
        {
            throw new DomainException(PlatformBillingErrors.LineInvalid);
        }

        string currency = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();

        if (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException(PlatformBillingErrors.CurrencyInvalid);
        }

        foreach (PlatformInvoiceLineInput line in lines)
        {
            PlatformInvoiceLine.Validate(line);
        }

        PlatformInvoice invoice = new(
            platformTenantId,
            platformSubscriptionId,
            kind,
            number,
            sourceKey,
            currency,
            language,
            customer,
            utcNow.AddDays(paymentTermsDays),
            periodStartUtc,
            periodEndUtc,
            utcNow);

        for (int index = 0; index < lines.Count; index++)
        {
            invoice._lines.Add(new PlatformInvoiceLine(invoice.Id, index + 1, lines[index], utcNow));
        }

        invoice.NetTotal = invoice._lines.Where(line => line.Kind != PlatformInvoiceLineKind.Discount).Sum(line => line.Net);
        invoice.DiscountTotal = -invoice._lines.Where(line => line.Kind == PlatformInvoiceLineKind.Discount).Sum(line => line.Net);
        invoice.TaxTotal = invoice._lines.Sum(line => line.Tax);
        invoice.Total = invoice.NetTotal - invoice.DiscountTotal + invoice.TaxTotal;

        if (invoice.Total <= 0m)
        {
            throw new DomainException(PlatformBillingErrors.NothingToBill);
        }

        invoice.Settle(utcNow);

        PlatformInvoiceEvent issued = PlatformInvoiceEvent.Record(
            invoice.Id,
            PlatformInvoiceEventCodes.Issued,
            null,
            utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["number"] = number,
                ["total"] = PlatformInvoiceEvent.Amount(invoice.Total),
                ["currency"] = currency
            });

        return new PlatformInvoiceIssue(invoice, issued);
    }

    // =====================================================
    // METHOD: PAYMENTS (D-B7)
    // =====================================================
    public PlatformInvoicePaymentResult RecordPayment(
        string receiptNumber,
        decimal amount,
        PlatformPaymentMethod method,
        DateTimeOffset receivedUtc,
        string? reference,
        string? note,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        RequireNotVoid();

        if (Status == PlatformInvoiceStatus.Paid)
        {
            throw new DomainException(PlatformBillingErrors.InvoicePaid);
        }

        if (!Enum.IsDefined(method))
        {
            throw new DomainException(PlatformBillingErrors.MethodNotAllowed);
        }

        if (amount <= 0m || !PlatformBillingLimits.IsMoney(amount))
        {
            throw new DomainException(PlatformBillingErrors.AmountInvalid);
        }

        if (amount > Balance)
        {
            throw new DomainException(PlatformBillingErrors.AmountAboveBalance);
        }

        string? trimmedReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        string? trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (trimmedReference is null && method != PlatformPaymentMethod.Cash)
        {
            throw new DomainException(PlatformBillingErrors.ReferenceRequired);
        }

        if (trimmedReference?.Length > PlatformBillingLimits.ReferenceMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.ReferenceTooLong);
        }

        if (trimmedNote?.Length > PlatformBillingLimits.NoteMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.NoteTooLong);
        }

        if (receivedUtc > utcNow || receivedUtc < IssuedUtc.AddYears(-1) || receivedUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(PlatformBillingErrors.ReceivedDateInvalid);
        }

        PlatformPayment payment = new(Id, receiptNumber, amount, method, receivedUtc, trimmedReference, trimmedNote, actorUserId, utcNow);
        _payments.Add(payment);
        bool wasOpen = Status == PlatformInvoiceStatus.Issued;
        Settle(utcNow);

        PlatformInvoiceEvent recorded = PlatformInvoiceEvent.Record(
            Id,
            PlatformInvoiceEventCodes.PaymentRecorded,
            actorUserId,
            utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = PlatformInvoiceEvent.Amount(amount),
                ["currency"] = CurrencyCode,
                ["method"] = method.ToString(),
                ["receipt"] = receiptNumber
            });

        return new PlatformInvoicePaymentResult(payment, recorded, wasOpen && Status == PlatformInvoiceStatus.Paid);
    }

    public PlatformInvoiceEvent ReversePayment(Guid paymentId, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = Actor(actorUserId);
        string why = Reason(reason);
        PlatformPayment payment = _payments.FirstOrDefault(item => item.Id == paymentId)
            ?? throw new DomainException(PlatformBillingErrors.PaymentNotFound);
        payment.Reverse(actor, why, utcNow);
        Settle(utcNow);

        return PlatformInvoiceEvent.Record(
            Id,
            PlatformInvoiceEventCodes.PaymentReversed,
            actor,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = PlatformInvoiceEvent.Amount(payment.Amount),
                ["currency"] = CurrencyCode,
                ["receipt"] = payment.ReceiptNumber
            });
    }

    // =====================================================
    // METHOD: CREDIT NOTES AND VOID (D-B6)
    // =====================================================
    public PlatformInvoiceEvent RequestCredit(decimal amount, bool isVoid, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = Actor(actorUserId);
        string why = Reason(reason);
        RequireNotVoid();

        if (PendingCredit is not null)
        {
            throw new DomainException(PlatformBillingErrors.CreditPending);
        }

        decimal remaining = Total - CreditedTotal;

        if (isVoid)
        {
            if (PaidTotal > 0m)
            {
                throw new DomainException(PlatformBillingErrors.VoidAfterPayment);
            }

            amount = remaining;
        }

        if (amount <= 0m || !PlatformBillingLimits.IsMoney(amount))
        {
            throw new DomainException(PlatformBillingErrors.AmountInvalid);
        }

        if (amount > remaining)
        {
            throw new DomainException(PlatformBillingErrors.CreditAboveRemaining);
        }

        decimal taxPart = Total == 0m ? 0m : PlatformBillingLimits.Money(amount * TaxTotal / Total);
        _credits.Add(new PlatformCreditNote(Id, amount, taxPart, isVoid, why, actor, utcNow));
        MarkAsModified(utcNow);

        return PlatformInvoiceEvent.Record(
            Id,
            isVoid ? PlatformInvoiceEventCodes.VoidRequested : PlatformInvoiceEventCodes.CreditRequested,
            actor,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["amount"] = PlatformInvoiceEvent.Amount(amount), ["currency"] = CurrencyCode });
    }

    public PlatformInvoiceCreditResult DecideCredit(Guid creditId, bool approve, string? number, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = Actor(actorUserId);
        PlatformCreditNote credit = _credits.FirstOrDefault(item => item.Id == creditId)
            ?? throw new DomainException(PlatformBillingErrors.CreditNotFound);
        string? why = string.IsNullOrWhiteSpace(reason) ? null : Reason(reason);

        if (!approve && why is null)
        {
            throw new DomainException(PlatformBillingErrors.ReasonRequired);
        }

        if (approve && (string.IsNullOrWhiteSpace(number) || number.Length > PlatformBillingLimits.NumberMaxLength))
        {
            throw new DomainException(PlatformBillingErrors.LineInvalid);
        }

        if (approve && Status == PlatformInvoiceStatus.Void)
        {
            throw new DomainException(PlatformBillingErrors.InvoiceVoid);
        }

        if (approve && credit.IsVoid && PaidTotal > 0m)
        {
            throw new DomainException(PlatformBillingErrors.VoidAfterPayment);
        }

        credit.Decide(actor, approve, number, why, utcNow);
        string code = !approve ? PlatformInvoiceEventCodes.CreditRejected : credit.IsVoid ? PlatformInvoiceEventCodes.Voided : PlatformInvoiceEventCodes.CreditIssued;

        if (approve)
        {
            Settle(utcNow);
        }
        else
        {
            MarkAsModified(utcNow);
        }

        Dictionary<string, string> arguments = new(StringComparer.Ordinal)
        {
            ["amount"] = PlatformInvoiceEvent.Amount(credit.Amount),
            ["currency"] = CurrencyCode
        };

        if (approve)
        {
            arguments["number"] = number!;
        }

        return new PlatformInvoiceCreditResult(credit, PlatformInvoiceEvent.Record(Id, code, actor, utcNow, why, arguments));
    }

    // =====================================================
    // METHOD: CARD REFUNDS AND DISPUTES (Slice 5, D-P6, D-P15)
    // =====================================================
    // The provider confirmed a refund to the card; it lowers what is owed back.
    public PlatformInvoiceEvent RecordCardRefund(decimal amount, string? reference, DateTimeOffset utcNow)
    {
        if (amount <= 0m || !PlatformBillingLimits.IsMoney(amount) || amount > RefundOwed)
        {
            throw new DomainException(PlatformBillingErrors.AmountAboveBalance);
        }

        RefundedTotal += amount;
        MarkAsModified(utcNow);

        return PlatformInvoiceEvent.Record(
            Id,
            PlatformInvoiceEventCodes.CardRefunded,
            null,
            utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = PlatformInvoiceEvent.Amount(amount),
                ["currency"] = CurrencyCode,
                ["reference"] = reference ?? string.Empty
            });
    }

    // A bank dispute opened, won or lost on a card payment; a lost one reverses the payment.
    public PlatformInvoiceEvent RecordDispute(Guid paymentId, string eventCode, DateTimeOffset utcNow)
    {
        PlatformPayment payment = _payments.FirstOrDefault(item => item.Id == paymentId)
            ?? throw new DomainException(PlatformBillingErrors.PaymentNotFound);

        if (eventCode is not (PlatformInvoiceEventCodes.Disputed or PlatformInvoiceEventCodes.DisputeWon or PlatformInvoiceEventCodes.DisputeLost))
        {
            throw new DomainException(PlatformBillingErrors.PaymentNotFound);
        }

        if (eventCode == PlatformInvoiceEventCodes.DisputeLost && payment.Counts)
        {
            payment.Reverse(null, null, utcNow);
            Settle(utcNow);
        }

        return PlatformInvoiceEvent.Record(
            Id,
            eventCode,
            null,
            utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = PlatformInvoiceEvent.Amount(payment.Amount),
                ["currency"] = CurrencyCode,
                ["receipt"] = payment.ReceiptNumber
            });
    }

    // A card charge failed or needs the cardholder: recorded for staff, the invoice is unchanged.
    public PlatformInvoiceEvent RecordCardAttempt(string eventCode, decimal amount, string? failureCode, DateTimeOffset utcNow)
    {
        if (eventCode is not (PlatformInvoiceEventCodes.CardFailed or PlatformInvoiceEventCodes.CardActionRequired))
        {
            throw new DomainException(PlatformBillingErrors.PaymentNotFound);
        }

        return PlatformInvoiceEvent.Record(
            Id,
            eventCode,
            null,
            utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = PlatformInvoiceEvent.Amount(amount),
                ["currency"] = CurrencyCode,
                ["code"] = failureCode ?? string.Empty
            });
    }

    // =====================================================
    // METHOD: PAST DUE (D-B8)
    // =====================================================
    public PlatformInvoiceEvent? MarkPastDueIfOverdue(DateTimeOffset utcNow)
    {
        if (PastDueUtc is not null || !IsOverdueAt(utcNow))
        {
            return null;
        }

        PastDueUtc = utcNow;
        MarkAsModified(utcNow);

        return PlatformInvoiceEvent.Record(Id, PlatformInvoiceEventCodes.PastDue, null, utcNow);
    }

    // =====================================================
    // METHOD: HELPERS
    // =====================================================
    private void Settle(DateTimeOffset utcNow)
    {
        PaidTotal = _payments.Where(item => item.Counts).Sum(item => item.Amount);
        CreditedTotal = _credits.Where(item => item.IsIssued).Sum(item => item.Amount);
        Balance = Math.Max(0m, Total - PaidTotal - CreditedTotal);

        if (_credits.Any(item => item.IsIssued && item.IsVoid))
        {
            Status = PlatformInvoiceStatus.Void;
        }
        else
        {
            Status = Balance == 0m ? PlatformInvoiceStatus.Paid : PlatformInvoiceStatus.Issued;
        }

        if (Status != PlatformInvoiceStatus.Issued)
        {
            PastDueUtc = null;
        }

        MarkAsModified(utcNow);
    }

    private void RequireNotVoid()
    {
        if (Status == PlatformInvoiceStatus.Void)
        {
            throw new DomainException(PlatformBillingErrors.InvoiceVoid);
        }
    }

    private static Guid Actor(Guid actorUserId)
    {
        return actorUserId == Guid.Empty ? throw new DomainException(PlatformBillingErrors.ActorRequired) : actorUserId;
    }

    private static string Reason(string? reason)
    {
        string trimmed = reason?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(PlatformBillingErrors.ReasonRequired);
        }

        return trimmed.Length > PlatformBillingLimits.ReasonMaxLength
            ? throw new DomainException(PlatformBillingErrors.ReasonTooLong)
            : trimmed;
    }
}
