using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Web.Components.Parcels;

// =========================================================
// FILE: OnHandFilter.cs
//
// PURPOSE:
// The filters of the on-hand list, as they came in the address (owner, 2026-10-22: parcel inventory).
//
// RESPONSIBILITIES:
// - Keep the filters together so the page passes one value, and build the address of another page
//
// IMPORTANT NOTES:
// - Only for showing and linking; the use case decides what the viewer may see
// =========================================================
public sealed record OnHandFilter(Guid? WarehouseId, Guid? LocationId, OperationsStorageZone? Zone, bool Unshelved, bool Held, int? MinDays, string? Search, int Page)
{
    public string Link(int page) => "/staff/parcels/on-hand?page=" + page
        + (WarehouseId is { } warehouse ? "&warehouse=" + warehouse : null)
        + (LocationId is { } location ? "&location=" + location : null)
        + (Zone is { } zone ? "&zone=" + zone.ToString().ToLowerInvariant() : null)
        + (Unshelved ? "&unshelved=true" : null)
        + (Held ? "&held=true" : null)
        + (MinDays is { } days ? "&days=" + days : null)
        + (string.IsNullOrWhiteSpace(Search) ? null : "&search=" + Uri.EscapeDataString(Search));
}
