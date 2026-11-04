using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsExtraVersion.cs
//
// PURPOSE:
// One dated amount of an extra charge: fixed or a percentage, per item, piece or shipment (Slice 4B design section 6).
//
// RESPONSIBILITIES:
// - Check and keep the amount, how it is worked out and what it counts
//
// IMPORTANT NOTES:
// - Stored inside the extra (JSON)
// - A percentage of the freight counts once per shipment
// =========================================================
public sealed class OperationsExtraVersion : IOperationsVersion
{
    private OperationsExtraVersion()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public decimal Amount { get; private set; }

    public OperationsExtraCalculation Calculation { get; private set; }

    public OperationsExtraPer Per { get; private set; }

    public static OperationsExtraVersion Create(decimal amount, OperationsExtraCalculation calculation, OperationsExtraPer per, DateTimeOffset effectiveFromUtc)
    {
        if (!Enum.IsDefined(calculation) || !Enum.IsDefined(per) || (calculation == OperationsExtraCalculation.PercentOfFreight && per != OperationsExtraPer.Shipment))
        {
            throw new DomainException(OperationsChargeErrors.ChoiceInvalid);
        }

        return new OperationsExtraVersion
        {
            Id = Guid.NewGuid(),
            EffectiveFromUtc = effectiveFromUtc,
            Amount = calculation == OperationsExtraCalculation.Fixed
                ? OperationsChargeLimits.Money(amount, OperationsChargeErrors.PriceInvalid)
                : OperationsChargeLimits.Percent(amount, OperationsChargeErrors.PercentInvalid),
            Calculation = calculation,
            Per = per
        };
    }
}
