namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupRow.cs
//
// PURPOSE:
// A shipment waiting at a pickup counter (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPickupRow(Guid ShipmentId, string Number, string? RecipientName, string? CollectorName, int Ready, DateTimeOffset ReadySince, bool Unpaid);
