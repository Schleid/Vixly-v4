using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsCustomerDeclaration.cs
//
// PURPOSE:
// The page the customer signs at the counter (Slice 5D, D-8): what they declare is in each parcel and its value, and the insurance they chose or declined.
//
// RESPONSIBILITIES:
// - Carry the shipment, the customer, the recipient and the route
// - Carry each parcel's contents, value and weight, and the totals
// - Carry the insurance product with its insured value, premium and main terms, or the company's liability without insurance
// - Carry the signature when the customer signed on screen
//
// IMPORTANT NOTES:
// - The company's name only, never "Vixly"
// - Printed in the customer's language
// =========================================================
public sealed record OperationsCustomerDeclaration(
    string CompanyName,
    string ShipmentNumber,
    DateOnly IssuedOn,
    string Customer,
    string Recipient,
    string Route,
    string? ServiceCode,
    IReadOnlyList<OperationsDeclarationLine> Lines,
    decimal TotalValue,
    string TotalWeight,
    string CurrencyCode,
    string? InsuranceName,
    decimal? InsuredValue,
    decimal? Premium,
    OperationsInsuranceCoverage? Coverage,
    string? LiabilityText,
    byte[]? Signature,
    DateTimeOffset? SignedUtc);
