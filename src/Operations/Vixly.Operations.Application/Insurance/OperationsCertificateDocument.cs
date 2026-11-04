using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;

namespace Vixly.Operations.Application.Insurance;

// =========================================================
// FILE: OperationsCertificateDocument.cs
//
// PURPOSE:
// The data of an insurance certificate as printed (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - Carry the certificate, the insured, the shipment, the cover's terms, the policy, the value and premium
//
// IMPORTANT NOTES:
// - Provisional: value and premium of the shipment today, fixed when it leaves (Slice 5)
// - The company's name only, never "Vixly"
// =========================================================
public sealed record OperationsCertificateDocument(
    string CompanyName,
    string CertificateNumber,
    OperationsCertificateStatus Status,
    DateOnly IssuedOn,
    DateTimeOffset AcceptedUtc,
    string ShipmentNumber,
    string Insured,
    string Consignee,
    string Route,
    OperationsTransportMode Mode,
    string? ServiceCode,
    string? InsurerName,
    string? PolicyNumber,
    bool SelfInsured,
    OperationsInsuredRisks Risks,
    OperationsInsuranceExclusions Exclusions,
    string? OtherExclusions,
    OperationsInsurancePayout Payout,
    decimal? DeductibleAmount,
    decimal? DeductiblePercent,
    decimal? MaxValue,
    decimal DeclaredValue,
    decimal? Premium,
    string CurrencyCode);
