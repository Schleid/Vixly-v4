using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformInvoiceLineItem.cs
//
// PURPOSE:
// One invoice line as shown.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformInvoiceLineItem(
    PlatformInvoiceLineKind Kind,
    string TextKey,
    IReadOnlyDictionary<string, string> Arguments,
    decimal Quantity,
    decimal UnitPrice,
    decimal Net,
    decimal TaxRate,
    string? TaxLabel,
    decimal Tax);
