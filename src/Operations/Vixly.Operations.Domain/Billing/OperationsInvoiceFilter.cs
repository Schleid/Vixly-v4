namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceFilter.cs
//
// PURPOSE:
// The states an invoice list can show (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Name the filters
//
// IMPORTANT NOTES:
// - Not stored
// =========================================================
public enum OperationsInvoiceFilter
{
    All = 0,

    // Something still to pay.
    Owed = 1,

    // With an approved credit note.
    Credited = 2,

    Voided = 3
}
