namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceTax.cs
//
// PURPOSE:
// The tax of one kind on an invoice (Slice 5F, I-4).
//
// RESPONSIBILITIES:
// - Keep the rate, the base it applied to and the amount
// - Keep how much of it was credited
//
// IMPORTANT NOTES:
// - The amount is the base times the rate, rounded to the cent
// =========================================================
public sealed class OperationsInvoiceTax
{
    private OperationsInvoiceTax()
    {
    }

    public OperationsInvoiceTax(OperationsTaxKind kind, decimal ratePercent, decimal basis, decimal amount)
    {
        (Kind, RatePercent, Basis, Amount) = (kind, ratePercent, basis, amount);
    }

    public OperationsTaxKind Kind { get; private set; }

    public decimal RatePercent { get; private set; }

    public decimal Basis { get; private set; }

    public decimal Amount { get; private set; }

    public decimal Credited { get; private set; }

    internal void Credit(decimal amount) => Credited += amount;
}
