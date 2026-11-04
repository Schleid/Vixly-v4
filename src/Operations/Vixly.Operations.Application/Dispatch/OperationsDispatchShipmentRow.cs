using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsDispatchShipmentRow.cs
//
// PURPOSE:
// A shipment on a dispatch as its page lists it (Slice 6B step 2).
//
// RESPONSIBILITIES:
// - Carry the row with the shipment's number, the customer's code and the consignee
//
// IMPORTANT NOTES:
// - The cost share is internal
// =========================================================
public sealed record OperationsDispatchShipmentRow(OperationsDispatchShipment Row, string ShipmentNumber, string CustomerCode, string Consignee);
