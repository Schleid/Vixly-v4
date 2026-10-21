namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceDraft.cs
//
// PURPOSE:
// What an invoice is issued from (Slice 5F, I-1 to I-4): the lines and everything kept with them.
//
// RESPONSIBILITIES:
// - Carry the lines, the parties, the reference and the settings of the day
//
// IMPORTANT NOTES:
// - Built by the application from a final shipment or a confirmed order
// =========================================================
public sealed record OperationsInvoiceDraft(
    OperationsInvoiceKind Kind,
    Guid CustomerId,
    Guid WarehouseId,
    Guid? ShipmentId,
    Guid? SupplySaleId,
    string Reference,
    string? Route,
    string? ServiceCode,
    string CurrencyCode,
    string Language,
    OperationsInvoiceParty Party,
    IReadOnlyList<OperationsInvoiceLine> Lines,
    string? TaxName,
    bool TaxPerKind,
    OperationsTaxRates Rates,
    string? Note);
