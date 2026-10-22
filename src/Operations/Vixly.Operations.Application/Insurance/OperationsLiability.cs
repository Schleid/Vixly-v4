using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Insurance;

// =========================================================
// FILE: OperationsLiability.cs
//
// PURPOSE:
// Finds what the company owes, per mode, when the customer did not insure (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - The liability terms the company set; else those of its insurance cover for the mode (Slice 4C); else none
//
// IMPORTANT NOTES:
// - The fallback keeps companies' earlier figures without copying data in a migration
// =========================================================
public sealed class OperationsLiability
{
    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public OperationsLiability(IOperationsInsuranceRepository insurance, IOperationsChargeRepository charges, IOperationsSettingsRepository settings, TimeProvider timeProvider)
    {
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IReadOnlyList<OperationsLiabilityView>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsLiabilityTerms> set = await _insurance.ListLiabilityAsync(cancellationToken);
        IReadOnlyList<OperationsInsurancePlan> plans = await _charges.ListInsurancePlansAsync(cancellationToken);
        OperationsWeightUnit unit = (await _settings.FindAsync(cancellationToken))?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<OperationsLiabilityView> views = new();

        foreach (OperationsTransportMode mode in Enum.GetValues<OperationsTransportMode>())
        {
            if (set.FirstOrDefault(item => item.Mode == mode) is { } terms)
            {
                // Shown and saved in the company's unit today: an amount per lb saved before the company moved to kg is converted (review).
                decimal? amount = terms.Basis == OperationsLiabilityBasis.PerWeight && terms.Amount is { } stored && terms.WeightUnit != unit
                    ? Math.Round(terms.WeightUnit == OperationsWeightUnit.Pound ? stored / 0.45359237m : stored * 0.45359237m, 4)
                    : terms.Amount;
                views.Add(new OperationsLiabilityView(mode, terms.Basis, amount, unit, terms.Cap, terms.CustomerText, false));
            }
            else if (plans.FirstOrDefault(plan => plan.Mode == mode)?.TermsAt(utcNow)?.Coverage is { } coverage
                && (coverage.LiabilityPerUnit is not null || coverage.LiabilityPerShipment is not null))
            {
                // Slice 4C wrote it as an amount per lb or kg and/or the most per shipment.
                views.Add(coverage.LiabilityPerUnit is { } perUnit
                    ? new OperationsLiabilityView(mode, OperationsLiabilityBasis.PerWeight, perUnit, unit, coverage.LiabilityPerShipment, null, true, coverage.LiabilityPerShipment is not null)
                    : new OperationsLiabilityView(mode, OperationsLiabilityBasis.DeclaredValueUpToCap, null, unit, coverage.LiabilityPerShipment, null, true, true));
            }
            else
            {
                views.Add(new OperationsLiabilityView(mode, OperationsLiabilityBasis.None, null, unit, null, null, false));
            }
        }

        return views;
    }
}
