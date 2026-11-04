namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyReceiptLine.cs
//
// PURPOSE:
// One line of supplies received at a warehouse (Slice 5E, S-5).
//
// RESPONSIBILITIES:
// - Carry the supply, how many and the cost of one
//
// IMPORTANT NOTES:
// - Cost optional; checked by the movement
// =========================================================
public sealed record OperationsSupplyReceiptLine(Guid SupplyId, int Quantity, decimal? UnitCost);
