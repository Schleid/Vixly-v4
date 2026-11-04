namespace Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;

// =========================================================
// FILE: PutAwayOperationsParcelRequest.cs
//
// PURPOSE:
// Puts a parcel on a shelf of its warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PutAwayOperationsParcelRequest
{
    public Guid ParcelId { get; init; }

    public Guid StorageLocationId { get; init; }

    public string CommandKey { get; init; } = string.Empty;

    // =====================================================
    // BY SCAN (owner, 2026-10-22: every move is scanned, the package then the shelf)
    // =====================================================

    // The package's number as scanned from its label, used when ParcelId is empty.
    public string? ParcelCode { get; init; }

    // The shelf's code as scanned from its label, used when StorageLocationId is empty.
    public string? ShelfCode { get; init; }
}
