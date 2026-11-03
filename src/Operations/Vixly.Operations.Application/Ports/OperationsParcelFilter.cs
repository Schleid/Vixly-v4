namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsParcelFilter.cs
//
// PURPOSE:
// What a staff parcel list asks for (Slice 3B).
//
// RESPONSIBILITIES:
// - Warehouses in the viewer's scope, a search, unidentified or held only
// - Parcel inventory (owner, 2026-10-22): only what is physically on hand, by shelf or zone, without a shelf, received before a date
//
// IMPORTANT NOTES:
// - The use case fills Warehouses from the viewer's grants; never from the request alone
// - On hand: at its origin warehouse and not yet departed (cancelled, returned and disposed parcels are gone)
// =========================================================
public sealed record OperationsParcelFilter(
    IReadOnlyList<Guid> Warehouses,
    string? Search,
    bool UnidentifiedOnly,
    bool HeldOnly,
    bool OnHandOnly = false,
    Guid? LocationId = null,
    Vixly.Operations.Domain.Receiving.OperationsStorageZone? Zone = null,
    bool UnshelvedOnly = false,
    DateTimeOffset? ReceivedBefore = null);
