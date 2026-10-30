namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplySaleStatus.cs
//
// PURPOSE:
// Where a supplies sale stands (Slice 5E, S-14 and S-15).
//
// RESPONSIBILITIES:
// - Name the states of a sale
//
// IMPORTANT NOTES:
// - Open: lines can change; confirmed: the stock moved (a shipment's supplies when it becomes final, an order when the clerk confirms it); cancelled
// =========================================================
public enum OperationsSupplySaleStatus
{
    Open = 1,
    Confirmed = 2,
    Cancelled = 3
}
