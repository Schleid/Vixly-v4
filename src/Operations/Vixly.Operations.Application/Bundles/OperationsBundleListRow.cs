namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleListRow.cs
//
// PURPOSE:
// A line of a bundle's list: entered from the computer, scanned or missing (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry its number, its shipment, whether it is a box and whether it was scanned
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsBundleListRow(Guid ItemId, bool IsBox, string Number, string? ShipmentNumber, bool Scanned, DateTimeOffset? ScannedUtc)
{
    // Missing and no longer able to go in (held, unpaid, moved, its box reopened): take it off the list (review).
    public bool Blocked { get; init; }
}
