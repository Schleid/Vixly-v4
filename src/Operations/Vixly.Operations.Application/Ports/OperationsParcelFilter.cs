namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsParcelFilter.cs
//
// PURPOSE:
// What a staff parcel list asks for (Slice 3B).
//
// RESPONSIBILITIES:
// - Warehouses in the viewer's scope, a search, unidentified or held only
//
// IMPORTANT NOTES:
// - The use case fills Warehouses from the viewer's grants; never from the request alone
// =========================================================
public sealed record OperationsParcelFilter(IReadOnlyList<Guid> Warehouses, string? Search, bool UnidentifiedOnly, bool HeldOnly);
