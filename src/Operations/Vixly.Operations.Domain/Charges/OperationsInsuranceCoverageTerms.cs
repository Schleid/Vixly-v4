namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceCoverageTerms.cs
//
// PURPOSE:
// The typed terms of an insurance cover, before they are checked (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Carry the values to OperationsInsuranceCoverage.Create
//
// IMPORTANT NOTES:
// - Money and days are checked by the coverage
// =========================================================
public sealed record OperationsInsuranceCoverageTerms(
    OperationsInsuranceCover Cover,
    string? InsurerName,
    string? PolicyNumber,
    string? InsurerContact,
    OperationsInsuredRisks Risks,
    OperationsInsuranceExclusions Exclusions,
    string? OtherExclusions,
    decimal? LiabilityPerUnit,
    decimal? LiabilityPerShipment,
    OperationsInsurancePayout Payout,
    decimal? DeductibleAmount,
    decimal? DeductiblePercent,
    decimal? MaxPerItem,
    int DamageClaimDays,
    int LossClaimDays,
    OperationsClaimProof Proof,
    OperationsClaimFiler FiledBy,
    string? TermsEn,
    string? TermsFr,
    string? TermsEs);
