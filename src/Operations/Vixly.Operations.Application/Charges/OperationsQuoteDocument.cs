using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsQuoteDocument.cs
//
// PURPOSE:
// What the quote PDF shows (owner, 2026-10-28: staff print it or email it to the customer).
//
// RESPONSIBILITIES:
// - The quote as saved, the customer, the route and the company's units
//
// IMPORTANT NOTES:
// - Drawn in the customer's language; the company's own name and logo, never "Vixly"
// =========================================================
public sealed record OperationsQuoteDocument(
    OperationsQuote Quote,
    string CompanyName,
    string CustomerName,
    string? CustomerCode,
    string CustomerEmail,
    string? Origin,
    string? Destination,
    OperationsWeightUnit WeightUnit,
    OperationsDimensionUnit DimensionUnit,
    DateOnly IssuedOn,
    DateOnly ValidUntil);
