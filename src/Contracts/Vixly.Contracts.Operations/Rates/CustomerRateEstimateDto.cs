namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerRateEstimateDto.cs
//
// PURPOSE:
// The estimate of one service (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the service, its lines and total
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Always an estimate: the final amount is set when the packages are measured
// =========================================================
public sealed record CustomerRateEstimateDto(string ServiceCode, string? Label, IReadOnlyList<CustomerChargeLineDto> Lines, decimal Total, bool InsuranceRefused);
