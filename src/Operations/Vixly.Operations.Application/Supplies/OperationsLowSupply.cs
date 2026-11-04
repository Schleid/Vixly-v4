namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsLowSupply.cs
//
// PURPOSE:
// A supply at or below its reorder level at a warehouse (Slice 5E, S-8).
//
// RESPONSIBILITIES:
// - Carry the warehouse, the supply and its stock against the level
//
// IMPORTANT NOTES:
// - Shown on the staff home page and the stock page
// =========================================================
public sealed record OperationsLowSupply(Guid WarehouseId, string WarehouseCode, Guid SupplyId, string SupplyCode, string SupplyName, int OnHand, int ReorderLevel);
