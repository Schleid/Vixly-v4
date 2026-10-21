namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceKind.cs
//
// PURPOSE:
// What an invoice is for (Slice 5F, F-1 and I-1).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsInvoiceKind
{
    // A shipment, with its charges, insurance and supplies.
    Shipment = 1,

    // An order of supplies on its own.
    SupplyOrder = 2
}
