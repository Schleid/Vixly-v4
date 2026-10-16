using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformInvoiceDraftLine.cs
//
// PURPOSE:
// A line to bill before tax is applied.
//
// RESPONSIBILITIES:
// - Carry kind, text, quantity and unit price
//
// IMPORTANT NOTES:
// - The issuer adds the tenant's tax rate (D-B1)
// =========================================================
public sealed record PlatformInvoiceDraftLine(
    PlatformInvoiceLineKind Kind,
    string TextKey,
    IReadOnlyDictionary<string, string> Arguments,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SourceId = null);
