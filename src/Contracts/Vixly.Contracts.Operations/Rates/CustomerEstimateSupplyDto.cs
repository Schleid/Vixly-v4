namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerEstimateSupplyDto.cs
//
// PURPOSE:
// A line of supplies in the customer's shipment estimate (Slice 5E, S-16).
//
// RESPONSIBILITIES:
// - Carry the supply, how many, the unit price, the discount and the amount
//
// IMPORTANT NOTES:
// - Added at the counter; the customer does not add supplies on the portal
// =========================================================
public sealed record CustomerEstimateSupplyDto(string Code, string Name, int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal Amount);
