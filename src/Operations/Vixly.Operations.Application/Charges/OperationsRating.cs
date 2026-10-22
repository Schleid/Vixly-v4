namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRating.cs
//
// PURPOSE:
// The charges of one shipment on one service: the lines and the total (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the result of the rating engine
//
// IMPORTANT NOTES:
// - InsuranceRefused: the customer asked for insurance and the value cannot be insured
// - VersionId: the price version used, so a quote or an invoice can name it
// =========================================================
public sealed record OperationsRating(string ServiceCode, Guid CardId, bool CustomerCard, IReadOnlyList<OperationsChargeLine> Lines, decimal Total, bool InsuranceRefused, Guid VersionId = default);
