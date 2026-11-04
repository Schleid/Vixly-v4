namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryWaitingRow.cs
//
// PURPOSE:
// A shipment waiting for home delivery at a warehouse (Slice 7 step 3, R-7, R-13).
//
// RESPONSIBILITIES:
// - Carry the values a page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsDeliveryWaitingRow(Guid ShipmentId, string Number, string? RecipientName, string? City, string? PostalCode, int Parcels, int Tries, decimal Due, string? CurrencyCode, bool Blocked);
