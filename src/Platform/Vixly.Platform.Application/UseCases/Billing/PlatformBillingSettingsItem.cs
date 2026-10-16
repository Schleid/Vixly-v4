using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformBillingSettingsItem.cs
//
// PURPOSE:
// Billing settings and the company profile as shown and edited.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformBillingSettingsItem(
    int? PaymentTermsDays,
    string InvoicePrefix,
    string CreditNotePrefix,
    string ReceiptPrefix,
    PlatformPaperSize PaperSize,
    bool EmailDocuments,
    IReadOnlyDictionary<string, PlatformBillingTextItem> Texts,
    PlatformCompanyProfileItem? Profile);
