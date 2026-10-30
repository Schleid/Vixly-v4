using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceVersion.cs
//
// PURPOSE:
// One dated version of the insurance plan (Slice 4B design section 6).
//
// RESPONSIBILITIES:
// - Check and keep the method, percentage, minimum, bands and maximum value
// - Give the premium for a declared value, or none when it cannot be insured
//
// IMPORTANT NOTES:
// - Stored inside the plan (JSON)
// =========================================================
public sealed class OperationsInsuranceVersion : IOperationsVersion
{
    private readonly List<OperationsInsuranceBand> _bands = new();

    private OperationsInsuranceVersion()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public OperationsInsuranceMethod Method { get; private set; }

    public decimal? Percent { get; private set; }

    public decimal? MinimumPremium { get; private set; }

    public IReadOnlyList<OperationsInsuranceBand> Bands => _bands;

    public decimal? MaxValue { get; private set; }

    // Slice 4C: who covers what, the exclusions, the payout and the claim rules.
    public OperationsInsuranceCoverage Coverage { get; private set; } = null!;

    public static OperationsInsuranceVersion Create(OperationsInsuranceTerms terms, DateTimeOffset effectiveFromUtc)
    {
        ArgumentNullException.ThrowIfNull(terms);

        if (!Enum.IsDefined(terms.Method))
        {
            throw new DomainException(OperationsChargeErrors.ChoiceInvalid);
        }

        List<OperationsInsuranceBand> bands = (terms.Bands ?? Array.Empty<OperationsInsuranceBand>())
            .Select(band => new OperationsInsuranceBand(OperationsChargeLimits.Money(band.UpToValue, OperationsChargeErrors.BandsInvalid),
                OperationsChargeLimits.Money(band.Premium, OperationsChargeErrors.BandsInvalid)))
            .OrderBy(band => band.UpToValue).ToList();

        bool valid = terms.Method == OperationsInsuranceMethod.Percent
            ? terms.Percent is > 0 && bands.Count == 0
            : terms.Percent is null && terms.MinimumPremium is null && bands.Count is > 0 and <= OperationsChargeLimits.MaxInsuranceBands
                && bands.Select(band => band.UpToValue).Distinct().Count() == bands.Count && bands.All(band => band.UpToValue > 0);

        if (!valid || terms.MaxValue is <= 0)
        {
            throw new DomainException(OperationsChargeErrors.InsuranceInvalid);
        }

        OperationsInsuranceVersion version = new()
        {
            Id = Guid.NewGuid(),
            EffectiveFromUtc = effectiveFromUtc,
            Method = terms.Method,
            Percent = terms.Percent is { } percent ? OperationsChargeLimits.Percent(percent, OperationsChargeErrors.InsuranceInvalid) : null,
            MinimumPremium = OperationsChargeLimits.Money(terms.MinimumPremium, OperationsChargeErrors.InsuranceInvalid),
            MaxValue = OperationsChargeLimits.Money(terms.MaxValue, OperationsChargeErrors.InsuranceInvalid),
            Coverage = OperationsInsuranceCoverage.Create(terms.Coverage ?? OperationsInsuranceCoverage.Defaults)
        };
        version._bands.AddRange(bands);
        return version;
    }

    // Slice 4C: null when a package is worth more than the most paid per item.
    public decimal? Premium(decimal declaredValue, IEnumerable<decimal> itemValues)
    {
        ArgumentNullException.ThrowIfNull(itemValues);
        return Coverage?.MaxPerItem is { } maxPerItem && itemValues.Any(value => value > maxPerItem) ? null : Premium(declaredValue);
    }

    // Null: this value cannot be insured (above the maximum, or above the last band).
    public decimal? Premium(decimal declaredValue)
    {
        if (declaredValue < 0 || (MaxValue is { } max && declaredValue > max))
        {
            return null;
        }

        return Method == OperationsInsuranceMethod.Percent
            ? Math.Max(declaredValue * Percent!.Value / 100m, MinimumPremium ?? 0m)
            : _bands.Where(band => band.UpToValue >= declaredValue).OrderBy(band => band.UpToValue).Select(band => (decimal?)band.Premium).FirstOrDefault();
    }
}
