namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyStockOut.cs
//
// PURPOSE:
// What the counter does when a supply has no stock left (Slice 5E, S-9).
//
// RESPONSIBILITIES:
// - Name the two company choices
//
// IMPORTANT NOTES:
// - Default: allow with a warning (the box is already in the customer's hands)
// =========================================================
public enum OperationsSupplyStockOut
{
    AllowWithWarning = 1,
    Refuse = 2
}
