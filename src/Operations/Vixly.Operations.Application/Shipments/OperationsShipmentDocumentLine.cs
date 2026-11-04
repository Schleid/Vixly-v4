namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentDocumentLine.cs
//
// PURPOSE:
// One parcel on a shipment document (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the parcel number, contents, value, weight and size as shown
//
// IMPORTANT NOTES:
// - Weight and size already in the company's units
// =========================================================
public sealed record OperationsShipmentDocumentLine(string ParcelNumber, string Contents, decimal? DeclaredValue, string? Weight, string? Size);
