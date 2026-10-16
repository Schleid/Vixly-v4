namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceLineInput.cs
//
// PURPOSE:
// What the billing use cases ask an invoice to charge.
//
// RESPONSIBILITIES:
// - Carry kind, text, quantity, unit price and tax for one line
//
// IMPORTANT NOTES:
// - Validated by PlatformInvoice.Create
// - A discount line has a negative unit price; every other line a positive one
// =========================================================
public sealed record PlatformInvoiceLineInput(
    PlatformInvoiceLineKind Kind,
    string TextKey,
    IReadOnlyDictionary<string, string> Arguments,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    string? TaxLabel,
    Guid? SourceId = null);
