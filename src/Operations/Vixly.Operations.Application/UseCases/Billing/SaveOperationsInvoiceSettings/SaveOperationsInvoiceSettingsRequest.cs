namespace Vixly.Operations.Application.UseCases.Billing.SaveOperationsInvoiceSettings;

// =========================================================
// FILE: SaveOperationsInvoiceSettingsRequest.cs
//
// PURPOSE:
// Change the company's invoice settings (Slice 5F, F-5, I-4 and I-10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsInvoiceSettingsRequest
{
    public string? TaxName { get; init; }

    public string? TaxNumber { get; init; }

    public decimal FreightTaxPercent { get; init; }

    public decimal InsuranceTaxPercent { get; init; }

    public decimal SuppliesTaxPercent { get; init; }

    public decimal ChargesTaxPercent { get; init; }

    // One tax line per kind on the invoice; otherwise a single line.
    public bool TaxPerKind { get; init; }

    public string? NoteEn { get; init; }

    public string? NoteFr { get; init; }

    public string? NoteEs { get; init; }
}
