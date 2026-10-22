namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditNoteLine.cs
//
// PURPOSE:
// One invoice line a credit note takes back, all or part of it (Slice 5F, I-6).
//
// RESPONSIBILITIES:
// - Keep the invoice line and the amount before tax
//
// IMPORTANT NOTES:
// - The label is the invoice line's, kept for the page and the PDF
// =========================================================
public sealed class OperationsCreditNoteLine
{
    private OperationsCreditNoteLine()
    {
    }

    public OperationsCreditNoteLine(Guid invoiceLineId, decimal amount)
    {
        (InvoiceLineId, Amount) = (invoiceLineId, amount);
    }

    public Guid InvoiceLineId { get; private set; }

    public decimal Amount { get; private set; }
}
