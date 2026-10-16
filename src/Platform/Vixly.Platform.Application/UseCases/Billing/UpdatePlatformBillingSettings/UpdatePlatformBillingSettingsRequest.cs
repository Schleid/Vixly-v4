using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing.UpdatePlatformBillingSettings;

// =========================================================
// FILE: UpdatePlatformBillingSettingsRequest.cs
//
// PURPOSE:
// Changes payment terms, number prefixes, paper size, the email switch and the invoice texts (D-B4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformBillingSettingsRequest
{
    public int? PaymentTermsDays { get; init; }

    public string? InvoicePrefix { get; init; }

    public string? CreditNotePrefix { get; init; }

    public string? ReceiptPrefix { get; init; }

    public PlatformPaperSize PaperSize { get; init; }

    public bool EmailDocuments { get; init; }

    public IReadOnlyDictionary<string, PlatformBillingTextItem> Texts { get; init; } = new Dictionary<string, PlatformBillingTextItem>();
}
