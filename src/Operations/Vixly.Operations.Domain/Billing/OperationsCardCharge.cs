using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardCharge.cs
//
// PURPOSE:
// One card payment asked of Stripe for an invoice (Slice 5F step 4): Stripe's payment page, a card reader, or the portal.
//
// RESPONSIBILITIES:
// - Keep what was asked (the invoice, the amount, the channel, who started it) and Stripe's references
// - Become paid once, on Stripe's word, with the payment it made; or cancelled
//
// IMPORTANT NOTES:
// - Paid never on the browser's word: Stripe's webhook or a check against Stripe (P-4)
// - One started charge per invoice at a time (unique filtered index); a new one cancels the old
// - The money Stripe took for an invoice that no longer owed it is kept as PaidNotApplied for staff to refund
// - With other methods first (owner, 2026-10-26): the card charges only what is left, and the other lines wait on the charge;
//   they become one payment with the card when Stripe confirms it, and nothing at all when the card is declined or abandoned.
//   One card per payment: the card is always the last line.
// =========================================================
public sealed class OperationsCardCharge : OperationsEntity
{
    private OperationsCardCharge()
    {
        CurrencyCode = string.Empty;
    }

    private OperationsCardCharge(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        CurrencyCode = string.Empty;
    }

    public Guid InvoiceId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public OperationsCardChannel Channel { get; private set; }

    public OperationsCardChargeStatus Status { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    // The staff member at the counter; none on the portal.
    public Guid? StartedByUserId { get; private set; }

    public Guid? ReaderId { get; private set; }

    // Stripe's payment page (cs_...) and payment (pi_...).
    public string? CheckoutSessionId { get; private set; }

    public string? PaymentIntentId { get; private set; }

    // Stripe's payment page address, shown again (QR code) while the charge waits.
    public string? PageUrl { get; private set; }

    public Guid? PaymentId { get; private set; }

    // The Stripe account's mode when it was started: a test payment never settles a live charge, nor the reverse (review).
    public bool Live { get; private set; }

    // Zero-decimal currencies (Stripe takes whole units only).
    // Currencies Stripe counts in whole units: an amount with cents cannot be charged or given back exactly.
    public static bool HasCentsInWholeUnitCurrency(string? currencyCode, decimal amount) =>
        ZeroDecimal.Contains(currencyCode, StringComparer.OrdinalIgnoreCase) && decimal.Truncate(amount) != amount;

    private static readonly string[] ZeroDecimal = { "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF" };

    public DateTimeOffset? EndedUtc { get; private set; }

    // The lines added before the card, recorded with it once Stripe confirms the card.
    // Null when stored empty (a charge from before, or a card for everything): read as no lines.
    private IReadOnlyList<OperationsTenderDraft>? _otherTenders;

    public IReadOnlyList<OperationsTenderDraft> OtherTenders
    {
        get => _otherTenders ?? Array.Empty<OperationsTenderDraft>();
        private set => _otherTenders = value;
    }

    public bool ZelleTermsAccepted { get; private set; }

    public decimal OtherTotal => OtherTenders.Sum(tender => tender.Amount);

    public static OperationsCardCharge Start(Guid tenantId, OperationsInvoice invoice, OperationsCardChannel channel, Guid? startedBy, Guid? readerId, bool live, DateTimeOffset utcNow,
        IReadOnlyList<OperationsTenderDraft>? others = null, bool zelleTermsAccepted = false)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        invoice.EnsureIssued();

        if (invoice.Owed <= 0m)
        {
            throw new DomainException(OperationsPaymentErrors.NothingOwed);
        }

        List<OperationsTenderDraft> before = (others ?? Array.Empty<OperationsTenderDraft>()).Where(tender => tender.Amount != 0m).ToList();
        CheckOthers(before, invoice.Owed, channel, zelleTermsAccepted);
        decimal amount = invoice.Owed - before.Sum(tender => tender.Amount);

        // Stripe cannot take a fraction of a yen (review): such an amount is paid another way.
        if (ZeroDecimal.Contains(invoice.CurrencyCode, StringComparer.OrdinalIgnoreCase) && decimal.Truncate(amount) != amount)
        {
            throw new DomainException(OperationsPaymentErrors.AmountInvalid);
        }

        if (!Enum.IsDefined(channel) || (channel == OperationsCardChannel.Reader) != (readerId is not null) || (channel == OperationsCardChannel.Portal) != (startedBy is null))
        {
            throw new DomainException(OperationsCardErrors.ReaderInvalid);
        }

        return new OperationsCardCharge(tenantId, utcNow)
        {
            InvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            WarehouseId = invoice.WarehouseId,
            Channel = channel,
            Status = OperationsCardChargeStatus.Started,
            Amount = amount,
            OtherTenders = before,
            ZelleTermsAccepted = zelleTermsAccepted && before.Any(tender => tender.Method == OperationsPaymentMethod.Zelle),
            CurrencyCode = invoice.CurrencyCode,
            StartedByUserId = startedBy,
            ReaderId = readerId,
            Live = live
        };
    }

    // The lines before the card: the same rules as a payment, checked again when it is recorded; the card is left something to charge.
    private static void CheckOthers(List<OperationsTenderDraft> before, decimal owed, OperationsCardChannel channel, bool zelleTermsAccepted)
    {
        if (before.Count == 0)
        {
            return;
        }

        if (channel == OperationsCardChannel.Portal || before.Count + 1 > OperationsPayment.MaxTenders
            || before.Any(tender => !Enum.IsDefined(tender.Method) || tender.Method == OperationsPaymentMethod.Card || tender.Amount < 0m || decimal.Round(tender.Amount, 2) != tender.Amount))
        {
            throw new DomainException(OperationsPaymentErrors.AmountInvalid);
        }

        if (before.Sum(tender => tender.Amount) >= owed)
        {
            throw new DomainException(OperationsCardErrors.NothingLeftForCard);
        }

        List<OperationsTenderDraft> cash = before.Where(tender => tender.Method == OperationsPaymentMethod.Cash).ToList();

        if (cash.Count > 1)
        {
            throw new DomainException(OperationsPaymentErrors.OneCashTender);
        }

        if (cash.Count == 1 && cash[0].CashGiven is { } given && (given < cash[0].Amount || decimal.Round(given, 2) != given))
        {
            throw new DomainException(OperationsPaymentErrors.CashGivenTooLow);
        }

        if (before.Any(tender => tender.Method == OperationsPaymentMethod.Zelle) && !zelleTermsAccepted)
        {
            throw new DomainException(OperationsPaymentErrors.ZelleTermsRequired);
        }
    }

    public void Sent(string? checkoutSessionId, string? paymentIntentId, string? pageUrl, DateTimeOffset utcNow)
    {
        if (pageUrl is not null && (pageUrl.Length > 2000 || !pageUrl.StartsWith("https://", StringComparison.Ordinal)))
        {
            throw new DomainException(OperationsCardErrors.ProviderRefused);
        }

        (CheckoutSessionId, PaymentIntentId, PageUrl) = (checkoutSessionId ?? CheckoutSessionId, paymentIntentId ?? PaymentIntentId, pageUrl ?? PageUrl);
        MarkAsModified(utcNow);
    }

    // Stripe says it is paid: the payment made for the invoice, or nothing applied when the invoice no longer owes this amount.
    // Also after a cancel (review): a page or reader paid at the last moment is still the customer's money, never lost.
    public void PaidBy(Guid? paymentId, string paymentIntentId, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsCardChargeStatus.Started or OperationsCardChargeStatus.Cancelled))
        {
            throw new DomainException(OperationsCardErrors.ChargeNotFound);
        }

        PaymentIntentId = string.IsNullOrWhiteSpace(paymentIntentId) ? PaymentIntentId : paymentIntentId;
        PaymentId = paymentId;
        Status = paymentId is null ? OperationsCardChargeStatus.PaidNotApplied : OperationsCardChargeStatus.Paid;
        EndedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        if (Status == OperationsCardChargeStatus.Started)
        {
            Status = OperationsCardChargeStatus.Cancelled;
            EndedUtc = utcNow;
            MarkAsModified(utcNow);
        }
    }
}
