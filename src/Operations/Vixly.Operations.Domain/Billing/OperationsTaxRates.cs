using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsTaxRates.cs
//
// PURPOSE:
// The company's tax rate for each kind of line (Slice 5F, F-5 and I-4).
//
// RESPONSIBILITIES:
// - Check the rates
// - Give the rate of a kind
//
// IMPORTANT NOTES:
// - A percentage from 0 to 100 with up to three decimals; 0 means no tax on that kind
// =========================================================
public sealed record OperationsTaxRates(decimal Freight, decimal Insurance, decimal Supplies, decimal Charges)
{
    public static OperationsTaxRates None { get; } = new(0m, 0m, 0m, 0m);

    public decimal Of(OperationsTaxKind kind) => kind switch
    {
        OperationsTaxKind.Freight => Freight,
        OperationsTaxKind.Insurance => Insurance,
        OperationsTaxKind.Supplies => Supplies,
        OperationsTaxKind.Charges => Charges,
        _ => 0m
    };

    public OperationsTaxRates Checked()
    {
        foreach (decimal rate in new[] { Freight, Insurance, Supplies, Charges })
        {
            if (rate is < 0m or > OperationsBillingLimits.MaxTaxPercent || decimal.Round(rate, 3) != rate)
            {
                throw new DomainException(OperationsBillingErrors.TaxRateInvalid);
            }
        }

        return this;
    }
}
