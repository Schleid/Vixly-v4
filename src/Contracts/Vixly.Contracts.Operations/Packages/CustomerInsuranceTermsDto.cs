namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerInsuranceTermsDto.cs
//
// PURPOSE:
// The insurance terms in force for one mode, which the customer accepts when insuring (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Carry who covers, the risks, the exclusions, the liability without insurance, the price, the payout and the claim rules, and the company's own text
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerInsuranceTermsDto(
    string Mode,
    string Cover,
    string? InsurerName,
    string? PolicyNumber,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Exclusions,
    string? OtherExclusions,
    decimal? LiabilityPerUnit,
    decimal? LiabilityPerShipment,
    string Method,
    decimal? Percent,
    decimal? MinimumPremium,
    string Payout,
    decimal? DeductibleAmount,
    decimal? DeductiblePercent,
    decimal? MaxPerItem,
    decimal? MaxValue,
    int DamageClaimDays,
    int LossClaimDays,
    IReadOnlyList<string> Proof,
    string FiledBy,
    string? TermsEn,
    string? TermsFr,
    string? TermsEs,
    string Currency = "",
    Guid? PlanId = null,
    string? Name = null,
    string? PitchEn = null,
    string? PitchFr = null,
    string? PitchEs = null);
