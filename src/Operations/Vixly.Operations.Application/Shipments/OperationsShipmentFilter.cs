namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentFilter.cs
//
// PURPOSE:
// What the shipments desk lists (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the warehouses (origins), the view, the service, the country and the search
//
// IMPORTANT NOTES:
// - Built by the use case; the warehouses are those the user may see
// =========================================================
public sealed record OperationsShipmentFilter(
    IReadOnlyCollection<Guid> Warehouses,
    OperationsShipmentView View,
    string? ServiceCode,
    string? CountryCode,
    string? Search);
