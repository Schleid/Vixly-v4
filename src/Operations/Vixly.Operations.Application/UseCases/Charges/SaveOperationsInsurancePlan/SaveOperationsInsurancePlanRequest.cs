using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsInsurancePlan;

// =========================================================
// FILE: SaveOperationsInsurancePlanRequest.cs
//
// PURPOSE:
// Sets the company's insurance plan, from now or from a later day: a percentage with a minimum, or value bands (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsInsurancePlanRequest
{
    // Slice 4C: one plan per mode.
    public OperationsTransportMode Mode { get; init; } = OperationsTransportMode.Air;

    // Slice 5D: the product changed; none: a new product for the mode.
    public Guid? PlanId { get; init; }

    public string? Name { get; init; }

    public string? PitchEn { get; init; }

    public string? PitchFr { get; init; }

    public string? PitchEs { get; init; }

    public int Position { get; init; }

    public OperationsInsuranceMethod Method { get; init; } = OperationsInsuranceMethod.Percent;

    public decimal? Percent { get; init; }

    public decimal? MinimumPremium { get; init; }

    public string? BandsText { get; init; }

    public decimal? MaxValue { get; init; }

    // Slice 4C: who covers what, the exclusions, the payout and the claim rules; null: the standard terms.
    public OperationsInsuranceCoverageTerms? Coverage { get; init; }

    public DateOnly? EffectiveFrom { get; init; }

    public bool Active { get; init; } = true;
}
