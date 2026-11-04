namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditNoteTax.cs
//
// PURPOSE:
// The tax of one kind a credit note gives back (Slice 5F, I-6).
//
// RESPONSIBILITIES:
// - Keep the kind and the amount
//
// IMPORTANT NOTES:
// - In proportion to the base credited; the last credit of a kind takes what is left
// =========================================================
public sealed class OperationsCreditNoteTax
{
    private OperationsCreditNoteTax()
    {
    }

    public OperationsCreditNoteTax(OperationsTaxKind kind, decimal amount)
    {
        (Kind, Amount) = (kind, amount);
    }

    public OperationsTaxKind Kind { get; private set; }

    public decimal Amount { get; private set; }
}
