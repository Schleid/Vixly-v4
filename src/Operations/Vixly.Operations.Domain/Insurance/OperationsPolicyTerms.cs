namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsPolicyTerms.cs
//
// PURPOSE:
// What a company records about one of its insurance policies (Slice 5B, design section 3).
//
// RESPONSIBILITIES:
// - Carry the scope, kind, insurer and broker, policy, limits, deductible, scope of cover, exclusions and duties
//
// IMPORTANT NOTES:
// - Checked by OperationsInsurancePolicy
// =========================================================
public sealed record OperationsPolicyTerms(
    OperationsPolicyScope Scope,
    OperationsPolicyKind Kind,
    string? InsurerName,
    string? InsurerEmail,
    string? InsurerPhone,
    string? ClaimsEmail,
    string? ClaimsPhone,
    string? BrokerName,
    string? BrokerEmail,
    string? BrokerPhone,
    string? PolicyNumber,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    string? CurrencyCode,
    decimal? LimitPerShipment,
    decimal? LimitPerConveyance,
    decimal? LimitPerLocation,
    decimal? LimitPerYear,
    decimal? DeductibleAmount,
    decimal? DeductiblePercent,
    decimal? DeductibleMinimum,
    OperationsPolicyRisks Risks,
    OperationsPolicyModes Modes,
    string? Territory,
    Vixly.Operations.Domain.Charges.OperationsInsuranceExclusions Exclusions,
    string? OtherExclusions,
    int? NoticeDays,
    OperationsDeclarationFrequency Declarations,
    string? Notes);
