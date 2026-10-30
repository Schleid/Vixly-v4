namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceTerms.cs
//
// PURPOSE:
// The terms of one version of the insurance plan, as staff set them (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the terms into a version in one value
//
// IMPORTANT NOTES:
// - The version checks every value
// =========================================================
public sealed record OperationsInsuranceTerms(
    OperationsInsuranceMethod Method,
    decimal? Percent,
    decimal? MinimumPremium,
    IReadOnlyList<OperationsInsuranceBand> Bands,
    decimal? MaxValue,
    OperationsInsuranceCoverageTerms? Coverage = null);
