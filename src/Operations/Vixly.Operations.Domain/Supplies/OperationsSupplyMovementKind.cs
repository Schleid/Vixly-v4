namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyMovementKind.cs
//
// PURPOSE:
// Why the stock of a supply changed (Slice 5E, S-4).
//
// RESPONSIBILITIES:
// - Name each kind of stock movement
//
// IMPORTANT NOTES:
// - Stored as a number; text comes from OperationsText (ui.supplies.movement.*)
// =========================================================
public enum OperationsSupplyMovementKind
{
    Received = 1,
    Sold = 2,
    SaleCancelled = 3,
    Adjusted = 4,
    TransferOut = 5,
    TransferIn = 6,
    Counted = 7
}
