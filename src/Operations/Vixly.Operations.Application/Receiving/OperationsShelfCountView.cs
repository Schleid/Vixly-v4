namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsShelfCountView.cs
//
// PURPOSE:
// A shelf count as its page shows it (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - The shelf, who and when, the codes scanned, how many are expected and, once finished, the four lists
//
// IMPORTANT NOTES:
// - Expected is the count of parcels the records put on the shelf now (open counts) or then (finished)
// =========================================================
public sealed record OperationsShelfCountView(Guid Id, Guid WarehouseId, string WarehouseName, string ShelfCode, bool IsOpen, DateTimeOffset StartedUtc, DateTimeOffset? FinishedUtc,
    IReadOnlyList<string> Scanned, int Expected, IReadOnlyList<string> Found, IReadOnlyList<string> Missing, IReadOnlyList<string> Extra, IReadOnlyList<string> Unknown);
