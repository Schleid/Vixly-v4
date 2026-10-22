namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsIncomingPackage.cs
//
// PURPOSE:
// One package coming to a customer, as staff see it on the customer's page (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - An announced package (no parcel yet) or a parcel at an origin warehouse, with its carrier number and contents
//
// IMPORTANT NOTES:
// - Never the mailbox's street address: staff need what is coming, not where the customer's mailbox is
// =========================================================
public sealed record OperationsIncomingPackage(Guid? ParcelId, string? ParcelNumber, string? Carrier, string TrackingNumber, string? Contents, string? WarehouseCode, bool OnHold,
    DateTimeOffset Since);
