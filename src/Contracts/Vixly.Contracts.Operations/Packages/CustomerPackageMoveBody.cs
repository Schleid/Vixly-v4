namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerPackageMoveBody.cs
//
// PURPOSE:
// Puts one of the customer's packages (a pre-alert or a received parcel) into one of its shipments, or takes it out (Slice 4D).
//
// RESPONSIBILITIES:
// - Carry the package and the shipment; no shipment: in none of the customer's shipments
//
// IMPORTANT NOTES:
// - Operations checks the ownership, the shipment and the warehouse
// =========================================================
public sealed record CustomerPackageMoveBody(Guid? PreAlertId, Guid? ParcelId, Guid? ShipmentId);
