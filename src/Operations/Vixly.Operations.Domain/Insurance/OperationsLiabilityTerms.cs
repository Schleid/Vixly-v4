using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsLiabilityTerms.cs
//
// PURPOSE:
// What the company owes, per transport mode, when the customer did not insure the shipment (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - Keep the basis, the amount, the cap and the words shown to the customer
// - Work out the most the company owes for one parcel
//
// IMPORTANT NOTES:
// - One per mode and company; the amount per weight is in the unit the company entered it in
// =========================================================
public sealed class OperationsLiabilityTerms : OperationsEntity
{
    public const int TextMaxLength = 2000;

    private OperationsLiabilityTerms()
    {
    }

    private OperationsLiabilityTerms(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Vixly.Operations.Domain.Charges.OperationsTransportMode Mode { get; private set; }

    public OperationsLiabilityBasis Basis { get; private set; }

    public decimal? Amount { get; private set; }

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private set; }

    public decimal? Cap { get; private set; }

    public string? CustomerText { get; private set; }

    public static OperationsLiabilityTerms Create(Guid tenantId, Vixly.Operations.Domain.Charges.OperationsTransportMode mode, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(OperationsInsuranceErrors.LiabilityInvalid);
        }

        return new OperationsLiabilityTerms(tenantId, utcNow) { Mode = mode, Basis = OperationsLiabilityBasis.None };
    }

    public void Change(OperationsLiabilityBasis basis, decimal? amount, Vixly.Operations.Domain.Settings.OperationsWeightUnit unit, decimal? cap, string? customerText,
        DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(basis) || !Enum.IsDefined(unit))
        {
            throw new DomainException(OperationsInsuranceErrors.LiabilityInvalid);
        }

        decimal? checkedAmount = OperationsInsuranceText.Amount(amount);
        decimal? checkedCap = OperationsInsuranceText.Amount(cap);

        // Each basis needs its figure: an amount per weight or parcel, a cap on the declared value.
        if ((basis is OperationsLiabilityBasis.PerWeight or OperationsLiabilityBasis.PerParcel && checkedAmount is not > 0)
            || (basis == OperationsLiabilityBasis.DeclaredValueUpToCap && checkedCap is not > 0))
        {
            throw new DomainException(OperationsInsuranceErrors.LiabilityInvalid);
        }

        (Basis, WeightUnit) = (basis, unit);
        Amount = basis is OperationsLiabilityBasis.PerWeight or OperationsLiabilityBasis.PerParcel ? checkedAmount : null;
        Cap = basis == OperationsLiabilityBasis.None ? null : checkedCap;
        CustomerText = OperationsInsuranceText.Optional(customerText, TextMaxLength, lines: true);
        MarkAsModified(utcNow);
    }

    // The most the company owes for one parcel, before any decision; weight in kilograms.
    public decimal Limit(decimal declaredValue, decimal? weightKg)
    {
        decimal owed = Basis switch
        {
            OperationsLiabilityBasis.PerWeight => (Amount ?? 0m) * (WeightUnit == Vixly.Operations.Domain.Settings.OperationsWeightUnit.Pound ? (weightKg ?? 0m) / 0.45359237m : weightKg ?? 0m),
            OperationsLiabilityBasis.PerParcel => Amount ?? 0m,
            OperationsLiabilityBasis.DeclaredValueUpToCap => declaredValue,
            _ => 0m
        };

        return decimal.Round(Cap is { } cap ? Math.Min(owed, cap) : owed, 2);
    }
}
