using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformBillingSettings.cs
//
// PURPOSE:
// How invoices are numbered, when they are due, how they are printed and whether they are emailed (D-B2, D-B4, D-B10, D-B13).
//
// RESPONSIBILITIES:
// - Hold payment terms, number prefixes, paper size, the email switch and the texts per language
//
// IMPORTANT NOTES:
// - One row (key "default"); protected by a row version
// - Until payment terms are set, invoices are due on the day they are issued (D-B4)
// =========================================================
public sealed class PlatformBillingSettings : AuditableEntity
{
    public const string SingletonKey = "default";

    public const string DefaultInvoicePrefix = "INV";

    public const string DefaultCreditNotePrefix = "CN";

    public const string DefaultReceiptPrefix = "RCT";

    private readonly List<PlatformBillingText> _texts = new();

    private PlatformBillingSettings()
    {
        Key = SingletonKey;
        InvoicePrefix = DefaultInvoicePrefix;
        CreditNotePrefix = DefaultCreditNotePrefix;
        ReceiptPrefix = DefaultReceiptPrefix;
        PaperSize = PlatformPaperSize.A4;
        EmailDocuments = true;
    }

    private PlatformBillingSettings(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = SingletonKey;
        InvoicePrefix = DefaultInvoicePrefix;
        CreditNotePrefix = DefaultCreditNotePrefix;
        ReceiptPrefix = DefaultReceiptPrefix;
        PaperSize = PlatformPaperSize.A4;
        EmailDocuments = true;
    }

    public string Key { get; private set; }

    public int? PaymentTermsDays { get; private set; }

    public string InvoicePrefix { get; private set; }

    public string CreditNotePrefix { get; private set; }

    public string ReceiptPrefix { get; private set; }

    public PlatformPaperSize PaperSize { get; private set; }

    public bool EmailDocuments { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public IReadOnlyList<PlatformBillingText> Texts => _texts;

    public int DueDays => PaymentTermsDays ?? 0;

    public static PlatformBillingSettings Create(DateTimeOffset utcNow)
    {
        return new PlatformBillingSettings(utcNow);
    }

    public string PrefixFor(PlatformBillingDocumentKind kind)
    {
        return kind switch
        {
            PlatformBillingDocumentKind.CreditNote => CreditNotePrefix,
            PlatformBillingDocumentKind.Receipt => ReceiptPrefix,
            _ => InvoicePrefix
        };
    }

    public PlatformBillingText? TextIn(LanguageCode language)
    {
        return _texts.FirstOrDefault(item => item.Language == language) ?? _texts.FirstOrDefault(item => item.Language == LanguageCode.Default);
    }

    public void Update(
        int? paymentTermsDays,
        string? invoicePrefix,
        string? creditNotePrefix,
        string? receiptPrefix,
        PlatformPaperSize paperSize,
        bool emailDocuments,
        IReadOnlyDictionary<LanguageCode, (string? PaymentInstructions, string? Footer)> texts,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformBillingErrors.ActorRequired);
        }

        if (paymentTermsDays is < 0 or > PlatformBillingLimits.MaxPaymentTermsDays || !Enum.IsDefined(paperSize))
        {
            throw new DomainException(PlatformBillingErrors.SettingInvalid);
        }

        string invoice = Prefix(invoicePrefix);
        string credit = Prefix(creditNotePrefix);
        string receipt = Prefix(receiptPrefix);

        if (invoice == credit || invoice == receipt || credit == receipt)
        {
            throw new DomainException(PlatformBillingErrors.PrefixInvalid);
        }

        List<PlatformBillingText> next = new();

        foreach ((LanguageCode language, (string? PaymentInstructions, string? Footer) text) in texts)
        {
            string? instructions = Text(text.PaymentInstructions);
            string? footer = Text(text.Footer);

            if (instructions is not null || footer is not null)
            {
                next.Add(new PlatformBillingText(language, instructions, footer));
            }
        }

        PaymentTermsDays = paymentTermsDays;
        InvoicePrefix = invoice;
        CreditNotePrefix = credit;
        ReceiptPrefix = receipt;
        PaperSize = paperSize;
        EmailDocuments = emailDocuments;
        UpdatedByUserId = actorUserId;
        _texts.Clear();
        _texts.AddRange(next);
        MarkAsModified(utcNow);
    }

    private static string Prefix(string? value)
    {
        string trimmed = value?.Trim().ToUpperInvariant() ?? string.Empty;

        return trimmed.Length is 0 or > PlatformBillingLimits.PrefixMaxLength || !trimmed.All(char.IsAsciiLetterUpper)
            ? throw new DomainException(PlatformBillingErrors.PrefixInvalid)
            : trimmed;
    }

    private static string? Text(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length > PlatformBillingLimits.BillingTextMaxLength
            ? throw new DomainException(PlatformBillingErrors.TextTooLong)
            : trimmed.Length == 0 ? null : trimmed;
    }
}
