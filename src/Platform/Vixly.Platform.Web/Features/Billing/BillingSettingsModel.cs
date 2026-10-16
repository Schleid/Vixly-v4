using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: BillingSettingsModel.cs
//
// PURPOSE:
// The editable copy of the billing settings.
//
// RESPONSIBILITIES:
// - Copy the settings in and give back the texts per language
//
// IMPORTANT NOTES:
// - Mutable form model; the use case validates
// =========================================================
public sealed class BillingSettingsModel
{
    public static IReadOnlyList<string> Languages { get; } = new[] { "en", "fr", "es" };

    public int? PaymentTermsDays { get; set; }

    public string InvoicePrefix { get; set; } = string.Empty;

    public string CreditNotePrefix { get; set; } = string.Empty;

    public string ReceiptPrefix { get; set; } = string.Empty;

    public PlatformPaperSize PaperSize { get; set; } = PlatformPaperSize.A4;

    public bool EmailDocuments { get; set; } = true;

    public List<TextRow> Texts { get; } = new();

    public static BillingSettingsModel From(PlatformBillingSettingsItem settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        BillingSettingsModel model = new()
        {
            PaymentTermsDays = settings.PaymentTermsDays,
            InvoicePrefix = settings.InvoicePrefix,
            CreditNotePrefix = settings.CreditNotePrefix,
            ReceiptPrefix = settings.ReceiptPrefix,
            PaperSize = settings.PaperSize,
            EmailDocuments = settings.EmailDocuments
        };

        foreach (string language in Languages)
        {
            PlatformBillingTextItem? text = settings.Texts.GetValueOrDefault(language);
            model.Texts.Add(new TextRow { Language = language, PaymentInstructions = text?.PaymentInstructions, Footer = text?.Footer });
        }

        return model;
    }

    public IReadOnlyDictionary<string, PlatformBillingTextItem> TextItems()
    {
        return Texts.ToDictionary(row => row.Language, row => new PlatformBillingTextItem(Blank(row.PaymentInstructions), Blank(row.Footer)), StringComparer.Ordinal);
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class TextRow
    {
        public string Language { get; set; } = string.Empty;

        public string? PaymentInstructions { get; set; }

        public string? Footer { get; set; }
    }
}
