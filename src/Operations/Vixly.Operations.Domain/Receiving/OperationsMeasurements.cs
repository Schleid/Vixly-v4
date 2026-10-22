using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsMeasurements.cs
//
// PURPOSE:
// Weight and size of a parcel (Slice 3B, D-O8).
//
// RESPONSIBILITIES:
// - Check the values; keep kilograms and centimetres
//
// IMPORTANT NOTES:
// - Entered in the tenant's units and converted before they get here
// - Scale and dimensioner fill the same values later
// =========================================================
public sealed record OperationsMeasurements
{
    private OperationsMeasurements(decimal weightKg, decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        (WeightKg, LengthCm, WidthCm, HeightCm) = (weightKg, lengthCm, widthCm, heightCm);
    }

    public decimal WeightKg { get; }

    public decimal LengthCm { get; }

    public decimal WidthCm { get; }

    public decimal HeightCm { get; }

    public static OperationsMeasurements Create(decimal weightKg, decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        bool sides = new[] { lengthCm, widthCm, heightCm }.All(side => side > 0 && side <= OperationsReceivingLimits.MaxSideCm);

        return weightKg > 0 && weightKg <= OperationsReceivingLimits.MaxWeightKg && sides
            ? new OperationsMeasurements(decimal.Round(weightKg, 3), decimal.Round(lengthCm, 1), decimal.Round(widthCm, 1), decimal.Round(heightCm, 1))
            : throw new DomainException(OperationsReceivingErrors.MeasurementsInvalid);
    }
}
