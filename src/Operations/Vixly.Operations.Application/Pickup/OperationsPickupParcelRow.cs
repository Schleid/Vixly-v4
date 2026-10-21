using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupParcelRow.cs
//
// PURPOSE:
// A parcel of a shipment at the pickup counter (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPickupParcelRow(Guid ParcelId, string Number, OperationsMilestone? Milestone, bool Here, bool Ready, bool CustomsHeld, decimal Storage);
