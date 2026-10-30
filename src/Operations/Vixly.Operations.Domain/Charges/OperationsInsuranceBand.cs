namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceBand.cs
//
// PURPOSE:
// One band of an insurance plan: up to this declared value, this premium (Slice 4 design section 5).
//
// RESPONSIBILITIES:
// - Keep the band's top value and premium
//
// IMPORTANT NOTES:
// - Stored inside the plan (JSON); the plan checks the bands
// =========================================================
public sealed class OperationsInsuranceBand
{
    private OperationsInsuranceBand()
    {
    }

    public OperationsInsuranceBand(decimal upToValue, decimal premium)
    {
        (UpToValue, Premium) = (upToValue, premium);
    }

    public decimal UpToValue { get; private set; }

    public decimal Premium { get; private set; }
}
