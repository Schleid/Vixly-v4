namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentCounts.cs
//
// PURPOSE:
// The counts beside the views of the shipments desk (Slice 5A).
//
// RESPONSIBILITIES:
// - Count each view under the same filters
//
// IMPORTANT NOTES:
// - Read only
// =========================================================
public sealed record OperationsShipmentCounts(int ToSend, int Waiting, int Cancelled);
