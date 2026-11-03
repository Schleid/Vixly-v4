using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsOnHandParcels;

// =========================================================
// FILE: ListOperationsOnHandParcelsRequest.cs
//
// PURPOSE:
// Lists the parcels physically on hand in the viewer's warehouses (owner, 2026-10-22: parcel inventory).
//
// RESPONSIBILITIES:
// - Carry the filters: warehouse, shelf or zone, without a shelf, on hold, at least so many days on hand, a search, the page
//
// IMPORTANT NOTES:
// - The warehouses come from the viewer's grants; a warehouse asked for outside them shows nothing
// =========================================================
public sealed class ListOperationsOnHandParcelsRequest
{
    public Guid? WarehouseId { get; init; }

    public Guid? LocationId { get; init; }

    public OperationsStorageZone? Zone { get; init; }

    public bool UnshelvedOnly { get; init; }

    public bool HeldOnly { get; init; }

    // At least this many days on hand (age filter).
    public int? MinDays { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;
}
