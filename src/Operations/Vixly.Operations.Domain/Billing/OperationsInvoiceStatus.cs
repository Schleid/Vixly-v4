namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceStatus.cs
//
// PURPOSE:
// Whether an invoice still stands (Slice 5F, I-5).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Credited, paid and owed are amounts on the invoice, not states
// =========================================================
public enum OperationsInvoiceStatus
{
    Issued = 1,
    Voided = 2
}
