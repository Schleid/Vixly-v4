using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsChargeLine.cs
//
// PURPOSE:
// One line of a rating: what it is, for which piece, the quantity, the price and the amount (Slice 4).
//
// RESPONSIBILITIES:
// - Carry a line to the quote, the estimate and later the invoice
//
// IMPORTANT NOTES:
// - Label: the extra's or item's own name; standard lines are named by their kind on the page (L-10)
// - Basis: the billable weight or volume of a freight line
// =========================================================
public sealed record OperationsChargeLine(
    OperationsChargeLineKind Kind,
    string? Label,
    int? Piece,
    decimal Quantity,
    decimal? Basis,
    decimal UnitPrice,
    decimal Amount,
    bool IsGovernmentFee = false,
    Guid? ExtraId = null);
