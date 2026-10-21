namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsRateBand.cs
//
// PURPOSE:
// One band of a price list: from this billable weight (or size), how it charges (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Keep the band's start, kind, amount and base
// - Give the charge for a billable weight in the band
//
// IMPORTANT NOTES:
// - Stored inside the price version (JSON); the version checks the bands
// - A band has a start only, so bands never leave gaps
// =========================================================
public sealed class OperationsRateBand
{
    private OperationsRateBand()
    {
    }

    public OperationsRateBand(decimal from, OperationsBandKind kind, decimal amount, decimal? @base = null)
    {
        (From, Kind, Amount, Base) = (from, kind, amount, @base);
    }

    // In the company's weight unit, or ft³ or m³ for a price by size.
    public decimal From { get; private set; }

    public OperationsBandKind Kind { get; private set; }

    public decimal Amount { get; private set; }

    public decimal? Base { get; private set; }

    public decimal Charge(decimal billable) => Kind switch
    {
        OperationsBandKind.Flat => Amount,
        OperationsBandKind.PerUnit => billable * Amount,
        _ => (Base ?? 0m) + (Math.Max(0m, billable - From) * Amount)
    };
}
