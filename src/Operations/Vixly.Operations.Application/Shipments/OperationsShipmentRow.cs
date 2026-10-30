using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentRow.cs
//
// PURPOSE:
// One line of the shipments desk (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the shipment, its customer code, destination, service and what is in it
//
// IMPORTANT NOTES:
// - Read only
// =========================================================
public sealed record OperationsShipmentRow(
    Guid ShipmentId,
    string? Number,
    Guid OriginWarehouseId,
    string? CustomerCode,
    OperationsShipmentStatus Status,
    string? CountryCode,
    string? City,
    string? RecipientName,
    string? ServiceCode,
    int Announced,
    int Parcels,
    int Labelled,
    DateTimeOffset CreatedUtc);
