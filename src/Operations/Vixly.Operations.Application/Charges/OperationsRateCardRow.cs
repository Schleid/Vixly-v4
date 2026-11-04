using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRateCardRow.cs
//
// PURPOSE:
// A price list with its zone and customer, for the prices page (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the list and the names shown with it
//
// IMPORTANT NOTES:
// - Null zone: all zones; null customer: the company's standard prices
// =========================================================
public sealed record OperationsRateCardRow(OperationsRateCard Card, string? OriginName, string? DestinationName, string? CustomerCode, string? CustomerName);
