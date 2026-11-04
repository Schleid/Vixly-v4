using Vixly.Operations.Domain.Bundles;

namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsRepackShipmentRow.cs
//
// PURPOSE:
// A consolidated shipment waiting to travel (Slice 6A, D-4): how it goes, and its box.
//
// RESPONSIBILITIES:
// - Carry its parcels, its way and its box, if any
//
// IMPORTANT NOTES:
// - Switched: the ship manager chose its way; otherwise the company's
// =========================================================
public sealed record OperationsRepackShipmentRow(Guid ShipmentId, string? Number, Guid WarehouseId, int Parcels, OperationsConsolidationWay Way, bool Switched, Guid? BoxId, string? BoxNumber,
    OperationsRepackBoxStatus? BoxStatus, bool Paid);
