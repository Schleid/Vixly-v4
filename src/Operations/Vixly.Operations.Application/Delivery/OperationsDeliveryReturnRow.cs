namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryReturnRow.cs
//
// PURPOSE:
// A parcel going back to its origin after its last failed delivery (Slice 7 step 3, R-14).
//
// RESPONSIBILITIES:
// - Carry the values a page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsDeliveryReturnRow(Guid ParcelId, string Number, Guid ShipmentId, string ShipmentNumber, string? RecipientName, int Tries);
